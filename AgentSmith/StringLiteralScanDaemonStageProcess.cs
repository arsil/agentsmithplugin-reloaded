using System;
using System.Collections.Generic;

using AgentSmith.Options;
using AgentSmith.SpellCheck;
using AgentSmith.SpellCheck.NetSpell;
using AgentSmith.Strings;

using JetBrains.Application.Settings;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Parsing;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Services;
using JetBrains.ReSharper.Psi.Tree;

namespace AgentSmith
{
    public class StringLiteralScanDaemonStageProcess : IDaemonStageProcess
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
        public StringLiteralScanDaemonStageProcess(IDaemonProcess daemonProcess, IContextBoundSettingsStore settingsStore)
        {
            this._daemonProcess = daemonProcess;
            this._solution = daemonProcess.Solution;

            this._settingsStore = settingsStore;


        }

        #region IDaemonStageProcess Members

        /// <summary>
        /// The current instance process that this stage is a part of.
        /// </summary>
        public IDaemonProcess DaemonProcess { get { return this._daemonProcess; } }

        /// <summary>
        /// Execute this stage of the process.
        /// </summary>
        /// <param name="commiter">The function to call when we've finished the stage to report the results.</param>
        public void Execute(Action<DaemonStageResult> commiter)
        {

            var highlightings = new List<HighlightingInfo>();

            IFile file = this._daemonProcess.SourceFile.GetTheOnlyPsiFile(CSharpLanguage.Instance);
            if (file == null)
            {
                return;
            }

            StringSettings stringSettings = this._settingsStore.GetKey<StringSettings>(SettingsOptimization.OptimizeDefault);


            file.ProcessChildren<ICSharpLiteralExpression>(literalExpression => this.CheckString(literalExpression, highlightings, stringSettings));

            try
            {
                commiter(new DaemonStageResult(highlightings));
            }
            catch
            {
                // Do nothing if it doesn't work.
            }
        }


        public void CheckString(ICSharpLiteralExpression literalExpression,
                                List<HighlightingInfo> highlightings, StringSettings settings)
        {
            //ConstantValue val = literalExpression.ConstantValue;

            // Ignore it unless it's something we're re-evalutating
            if (!this._daemonProcess.IsRangeInvalidated(literalExpression.GetDocumentRange())) return;



            // Ignore verbatim strings.
            if (settings.IgnoreVerbatimStrings &&
                LiteralService.Get(CSharpLanguage.Instance).IsVerbatimStringLiteral(literalExpression)) return;

            ITokenNode tokenNode = literalExpression.Literal;
            if (tokenNode == null) return;

            if (tokenNode.GetTokenType() == CSharpTokenType.STRING_LITERAL)
            {
                ISpellChecker spellChecker = SpellCheckManager.GetSpellChecker(this._settingsStore, this._solution, settings.DictionaryNames);

                StringSpellChecker.SpellCheck(
                    literalExpression.GetDocumentRange().Document,
                    tokenNode,
                    spellChecker,
                    this._solution, highlightings, this._settingsStore, settings);
            }
        }
        #endregion
    }
}