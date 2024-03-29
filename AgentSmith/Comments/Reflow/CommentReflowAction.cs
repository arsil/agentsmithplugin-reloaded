using System;

using AgentSmith.Options;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.Application.Progress;
using JetBrains.Application.Settings;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.CSharp.Bulbs;
using JetBrains.ReSharper.Intentions.Extensibility;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;
using JetBrains.Util;

namespace AgentSmith.Comments.Reflow
{
        
    [ContextAction(Group = "C#", Name = "Reflow comment", Description = "Reflow comment.")]
    internal class CommentReflowAction : ContextActionBase
    {

        protected readonly IContextActionDataProvider Provider;
        private IDocCommentNode _selectedDocCommentNode;


        public CommentReflowAction(ICSharpContextActionDataProvider provider)
        {
            Provider = provider;
        }

        private static int CalcLineOffset( IDocCommentBlockOwnerNode node )
        {
            ITreeNode prev = node.PrevSibling;
            if ( prev != null && prev is IWhitespaceNode &&
                 !( (IWhitespaceNode)prev ).IsNewLine )
            {
                return prev.GetTextLength();
            }
            return 0;
        }

        protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
        {
            IDocCommentNode docCommentNode = _selectedDocCommentNode;

            ReFlowCommentNode(solution, progress, docCommentNode);
            return null;
        }

        public static void ReFlowCommentNode(ISolution solution, IProgressIndicator progress, [NotNull] IDocCommentNode docCommentNode)
        {
            // Get the comment block owner (ie the part of the declaration which will own the comment).
            IDocCommentBlockNode blockNode =
                docCommentNode.GetContainingNode<IDocCommentBlockNode>();
            if (blockNode == null) return;

            ReFlowCommentBlockNode(solution, progress, blockNode);
        }

        public static void ReFlowCommentBlockNode(ISolution solution, IProgressIndicator progress, IDocCommentBlockNode docCommentBlockNode)
        {
            if (docCommentBlockNode == null) return;

            // Get the settings.
            IContextBoundSettingsStore settingsStore = Shell.Instance.GetComponent<ISettingsStore>().BindToContextTransient(ContextRange.ApplicationWide);
            XmlDocumentationSettings settings =
                settingsStore.GetKey<XmlDocumentationSettings>(SettingsOptimization.OptimizeDefault);
            ReflowAndRetagSettings reflowSettings =
                settingsStore.GetKey<ReflowAndRetagSettings>(SettingsOptimization.OptimizeDefault);
            int maxLength = settings.MaxCharactersPerLine;

            IDocCommentBlockOwnerNode ownerNode =
                docCommentBlockNode.GetContainingNode<IDocCommentBlockOwnerNode>();

            // If we didn't get an owner then give up
            if (ownerNode == null) return;

            // Get a factory which can create elements in the C# docs
            //CSharpElementFactory factory = CSharpElementFactory.GetInstance(ownerNode.GetPsiModule());

            // Calculate line offset where /// starts and add 3 for each
            // slash.
            int startPos = CalcLineOffset(ownerNode) + 3;

            // Create a new comment block with the adjusted text
            IDocCommentBlockNode comment = docCommentBlockNode; //factory.CreateDocCommentBlock(text);

            // Work out if we have a space between the /// and <summary>
            string reflownText = new XmlCommentReflower(settings, reflowSettings).Reflow(comment, maxLength - startPos);

            //comment = factory.CreateDocCommentBlock(reflownText);

            SetDocComment(ownerNode, reflownText, solution);

            // And set the comment on the declaration.
            //ownerNode.SetDocCommentBlockNode(comment);
            
        }


        public static void SetDocComment(IDocCommentBlockOwnerNode docCommentBlockOwnerNode, string text, ISolution solution)
        {
            text = String.Format("///{0}\r\nclass Tmp {{}}", text.Replace("\n", "\n///"));

            ICSharpTypeMemberDeclaration declaration =
                CSharpElementFactory.GetInstance(docCommentBlockOwnerNode.GetPsiModule()).CreateTypeMemberDeclaration(text, new object[0]);
            docCommentBlockOwnerNode.SetDocCommentBlockNode(
                ((IDocCommentBlockOwnerNode)declaration).GetDocCommentBlockNode());
        }


        public override string Text
        {
            get { return "Reflow Comment [Agent Smith]"; }
        }

        private T GetSelectedExpression<T>() where T: class, ITreeNode
        {
            return Provider.GetSelectedElement<T>(true, true);
        }

        public override bool IsAvailable(IUserDataHolder cache)
        {
            using (ReadLockCookie.Create())
            {
                this._selectedDocCommentNode = GetSelectedExpression<IDocCommentNode>();

                return this._selectedDocCommentNode != null;
            }
        }
    }
}