using System;

using AgentSmith.Options;

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
    [ContextAction(Group = "C#", Name = "Reflow & Retag Comment", Description = "Reflow & Retag Comment.")]
    internal class CommentReflowAndRetagAction : ContextActionBase
    {

        protected readonly IContextActionDataProvider Provider;
        private IDocCommentNode _selectedDocCommentNode;


        public CommentReflowAndRetagAction(ICSharpContextActionDataProvider provider)
        {
            Provider = provider;
        }

        private static int CalcLineOffset(IDocCommentBlockOwnerNode node)
        {
            ITreeNode prev = node.PrevSibling;
            if (prev != null && prev is IWhitespaceNode &&
                !((IWhitespaceNode)prev).IsNewLine)
            {
                return prev.GetTextLength();
            }
            return 0;
        }

        public static void ReflowAndRetagCommentNode(ISolution solution, IProgressIndicator progress, IDocCommentNode docCommentNode)
        {
            if (docCommentNode == null) return;

            IDocCommentBlockNode blockNode =
                docCommentNode.GetContainingNode<IDocCommentBlockNode>();

            if (blockNode == null) return;

            ReflowAndRetagCommentBlockNode(solution, progress, blockNode);
        }

        public static void ReflowAndRetagCommentBlockNode(ISolution solution, IProgressIndicator progress,  IDocCommentBlockNode docCommentBlockNode)
        {
            if (docCommentBlockNode == null) return;

            // Get the settings.
            IContextBoundSettingsStore settingsStore = Shell.Instance.GetComponent<ISettingsStore>().BindToContextTransient(ContextRange.ApplicationWide);
            XmlDocumentationSettings settings =
                settingsStore.GetKey<XmlDocumentationSettings>(SettingsOptimization.OptimizeDefault);
            ReflowAndRetagSettings reflowSettings =
                settingsStore.GetKey<ReflowAndRetagSettings>(SettingsOptimization.OptimizeDefault);
            int maxLength = settings.MaxCharactersPerLine;

            // Get the comment block owner (ie the part of the declaration which will own the comment).
            IDocCommentBlockOwnerNode ownerNode =
                docCommentBlockNode.GetContainingNode<IDocCommentBlockOwnerNode>();

            // If we didn't get an owner then give up
            if (ownerNode == null) return;

            // Get a factory which can create elements in the C# docs
            //CSharpElementFactory factory = CSharpElementFactory.GetInstance(ownerNode.GetPsiModule());

            // Calculate line offset where /// starts and add 4 for the slashes and space
            int startPos = CalcLineOffset(ownerNode) + 4;

            // Create a new comment block with the adjusted text
            IDocCommentBlockNode comment = docCommentBlockNode; //factory.CreateDocCommentBlock(text);

            string reflownText = new XmlCommentReflower(settings, reflowSettings).ReflowAndRetag(comment, maxLength - startPos);

            // If the xml was malformed then the comment will now be empty - detect this and do nothing
            if (string.IsNullOrEmpty(reflownText)) return;

            /*comment = factory.CreateDocCommentBlock(reflownText);

            // And set the comment on the declaration.
            ownerNode.SetDocCommentBlockNode(comment);*/

            SetDocComment(ownerNode, reflownText, solution);
        }

        protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
        {
            IDocCommentNode docCommentNode = _selectedDocCommentNode;
            if (docCommentNode == null) return null;

            ReflowAndRetagCommentNode(solution, progress, docCommentNode);

            return null;
        }

        public static void SetDocComment(IDocCommentBlockOwnerNode docCommentBlockOwnerNode, string text, ISolution solution)
        {
            text = String.Format("/// {0}\r\nclass Tmp {{}}", text.Replace("\n", "\n/// "));

            ICSharpTypeMemberDeclaration declaration =
                CSharpElementFactory.GetInstance(docCommentBlockOwnerNode.GetPsiModule()).CreateTypeMemberDeclaration(text, new object[0]);
            docCommentBlockOwnerNode.SetDocCommentBlockNode(
                ((IDocCommentBlockOwnerNode)declaration).GetDocCommentBlockNode());
        }


        public override string Text
        {
            get { return "Reflow & Retag Comment [Agent Smith]"; }
        }

        private T GetSelectedExpression<T>() where T : class, ITreeNode
        {
            return Provider.GetSelectedElement<T>(true, true);
        }

        public override bool IsAvailable(IUserDataHolder cache)
        {
            using (ReadLockCookie.Create())
            {
                _selectedDocCommentNode = GetSelectedExpression<IDocCommentNode>();

                return _selectedDocCommentNode != null;
            }
        }

    }
}