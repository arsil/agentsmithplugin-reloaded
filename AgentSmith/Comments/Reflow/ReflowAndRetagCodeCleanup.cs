using System;
using System.Collections.Generic;
using System.ComponentModel;
using JetBrains.Application;
using JetBrains.Application.Progress;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.CodeCleanup;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.Tree;

namespace AgentSmith.Comments.Reflow
{
    [CodeCleanupModule]
    public class ReflowAndRetagCodeCleanup : ICodeCleanupModule
    {
        private readonly IShellLocks _shellLocks;
        private static readonly Descriptor DescriptorInstance = new Descriptor();

        public ReflowAndRetagCodeCleanup(IShellLocks shellLocks)
        {
            _shellLocks = shellLocks;
        }

        public void SetDefaultSetting(CodeCleanupProfile profile, CodeCleanup.DefaultProfileType profileType)
        {
            switch (profileType)
            {
                case CodeCleanup.DefaultProfileType.FULL:
                    profile.SetSetting(DescriptorInstance, true);
                    break;

                case CodeCleanup.DefaultProfileType.REFORMAT:
                    profile.SetSetting(DescriptorInstance, false);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("profileType");
            }
        }

        public bool IsAvailable(IPsiSourceFile sourceFile)
        {
            return sourceFile.GetNonInjectedPsiFile<CSharpLanguage>() != null;
        }


        public void Process(IPsiSourceFile sourceFile, IRangeMarker rangeMarker, CodeCleanupProfile profile, IProgressIndicator progressIndicator)
        {
			IFile file = sourceFile.GetNonInjectedPsiFile<CSharpLanguage>();
            if (file == null)
                return;

            if (!profile.GetSetting(DescriptorInstance))
                return;

            file.GetPsiServices().PsiManager.DoTransaction(
                () =>
                {
                    using (_shellLocks.UsingWriteLock())
                        file.ProcessChildren<IDocCommentBlockOwnerNode>(x =>
                            CommentReflowAndRetagAction.ReflowAndRetagCommentBlockNode(x.GetSolution(), null, x.GetDocCommentBlockNode())
                            );
                },
                "Reflow & Retag XML Documentation Comments");
        }

        public PsiLanguageType LanguageType
        {
            get { return CSharpLanguage.Instance; }
        }

        public ICollection<CodeCleanupOptionDescriptor> Descriptors
        {
            get { return new CodeCleanupOptionDescriptor[] { DescriptorInstance }; }
        }

        public bool IsAvailableOnSelection
        {
            get { return false; }
        }

        [DefaultValue(false)]
        [DisplayName("Reflow & Retag XML Documentation Comments")]
        [Category(CSharpCategory)]
        private class Descriptor : CodeCleanupBoolOptionDescriptor
        {
            public Descriptor() : base("AgentSmithReflowAndRetagXMLDoc") { }
        }
    }
}
