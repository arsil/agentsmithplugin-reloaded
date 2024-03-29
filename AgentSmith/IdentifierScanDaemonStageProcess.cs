﻿using System;
using System.Collections.Generic;
using System.Xml;

using AgentSmith.Comments;
using AgentSmith.Identifiers;
using AgentSmith.Options;

using JetBrains.Application.Settings;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Daemon.Stages;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Tree;

namespace AgentSmith
{
    /// <summary>
    /// Process stage for analysing comments in a project file.
    /// </summary>
    /// <remarks>
    /// <para>
    /// We scan for all declarations.
    /// </para>
    /// <para>
    /// For each declaration we do several checks:
    /// </para>
    /// <list type="bullet">
    /// <item>
    /// Does the member have an XML documentation comment?
    /// </item>
    /// <item>
    /// Are all the words in the comment correctly spelled?
    /// </item>
    /// <item>
    /// Are there any words that look like identifiers that we should wrap with meta-tags?
    /// </item>
    /// </list>
    /// </remarks>
    internal class IdentifierScanDaemonStageProcess : IDaemonStageProcess
    {
        /// <summary>
        /// Internal storage for the process that this stage is a part of
        /// </summary>
        private readonly IDaemonProcess _daemonProcess;
        private readonly ISolution _solution;

        private readonly IContextBoundSettingsStore _settingsStore;

        /// <summary>
        /// Create a new process within a stage that processes comments in the current file.
        /// </summary>
        /// <param name="daemonProcess">The current instance process that this stage will be a part of</param>
        /// <param name="settingsStore"> </param>
        public IdentifierScanDaemonStageProcess(IDaemonProcess daemonProcess, IContextBoundSettingsStore settingsStore)
        {
            _daemonProcess = daemonProcess;
            _solution = daemonProcess.Solution;

            _settingsStore = settingsStore;


        }

        #region IDaemonStageProcess Members

        /// <summary>
        /// The current instance process that this stage is a part of.
        /// </summary>
        public IDaemonProcess DaemonProcess { get { return _daemonProcess; } }



        private void CheckMember(
            IClassMemberDeclaration declaration, 
            IHighlightingConsumer highlightingConsumer, 
            CommentAnalyzer commentAnalyzer, 
            IdentifierSpellCheckAnalyzer identifierAnalyzer)
        {
            if (declaration is IConstructorDeclaration && declaration.IsStatic)
            {
                // TODO: probably need to put this somewhere in settings.
                //Static constructors have no visibility so not clear how to check them.
                return;
            }


            // Documentation doesn't work properly on multiple declarations (as of R# 6.1) so see if we can get it from the parent
            XmlNode docNode = null;
            IDocCommentBlockNode commentBlock;
            var multipleDeclarationMember = declaration as IMultipleDeclarationMember;
            if (multipleDeclarationMember != null)
            {
                // get the parent
                IMultipleDeclaration multipleDeclaration = multipleDeclarationMember.MultipleDeclaration;

                // Now ask for the actual comment block
                commentBlock = SharedImplUtil.GetDocCommentBlockNode(multipleDeclaration);

                if (commentBlock != null) docNode = commentBlock.GetXML(null);
            }
            else
            {
                commentBlock = SharedImplUtil.GetDocCommentBlockNode(declaration);

                docNode = declaration.GetXMLDoc(false);

            }

            commentAnalyzer.CheckMemberHasComment(declaration, docNode, highlightingConsumer);
            commentAnalyzer.CheckCommentSpelling(declaration, commentBlock, highlightingConsumer, true);
            identifierAnalyzer.CheckMemberSpelling(declaration, highlightingConsumer, true);
        }

        /// <summary>
        /// Execute this stage of the process.
        /// </summary>
        /// <param name="commiter">The function to call when we've finished the stage to report the results.</param>
        public void Execute(Action<DaemonStageResult> commiter)
        {

            //var highlightings = new List<HighlightingInfo>();
             IHighlightingConsumer highlightingConsumer = new DefaultHighlightingConsumer(
                 this, _settingsStore);

            IFile file = _daemonProcess.SourceFile.GetTheOnlyPsiFile(CSharpLanguage.Instance);
            if (file == null)
            {
                return;
            }

            if (!_daemonProcess.FullRehighlightingRequired) return;

            var commentAnalyzer = new CommentAnalyzer(_solution, _settingsStore);
            var identifierAnalyzer = new IdentifierSpellCheckAnalyzer(_solution, _settingsStore, _daemonProcess.SourceFile);

            file.ProcessChildren<IClassMemberDeclaration>(
                declaration => CheckMember(
                    declaration, highlightingConsumer, commentAnalyzer, identifierAnalyzer));

            if (_daemonProcess.InterruptFlag) return;
            try
            {
                commiter(new DaemonStageResult(highlightingConsumer.Highlightings));
            } catch
            {
                // Do nothing if it doesn't work.
            }
        }



        #endregion
    }
}