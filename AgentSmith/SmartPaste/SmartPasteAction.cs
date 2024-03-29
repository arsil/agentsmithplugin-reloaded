using System;
using System.Windows.Forms;
using AgentSmith.Comments;
using JetBrains.ActionManagement;
using JetBrains.Application.DataContext;
using JetBrains.DocumentModel;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Parsing;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;
using JetBrains.Util;
using JetBrains.Util.Logging;

using MessageBox=JetBrains.Util.MessageBox;

using JetBrains.ReSharper.Psi.Files;

namespace AgentSmith.SmartPaste
{
    [ActionHandler(new[] { "AgentSmith.SmartPaste" })]
    internal class SmartInsertAction : IActionHandler
    {
        #region IActionHandler Members
        
        public bool Update(IDataContext context, ActionPresentation presentation, DelegateUpdate nextUpdate)
        {
            bool update = nextUpdate();

            if (!IsAvailable(context))
            {
                return update;
            }
            return true;
        }

        public void Execute(IDataContext context, DelegateExecute nextExecute)
        {
            if (!IsAvailable(context))
            {
                nextExecute();
            }
            else
            {
                
                ISolution solution = context.GetData(JetBrains.ProjectModel.DataContext.DataConstants.SOLUTION);
                if (solution == null)
                {
                    nextExecute();
                    return;
                }
                solution.GetPsiServices()
	                    .Transactions.Execute("SmartPaste", () => ExecuteEx(solution, context));
            }
        }

        public void ExecuteEx(ISolution solution, IDataContext context)
        {
            ITextControl editor = context.GetData(JetBrains.TextControl.DataContext.DataConstants.TEXT_CONTROL);
            Logger.Assert(editor != null, "Condition (editor != null) is false");

			IDocument document = context.GetData(JetBrains.DocumentModel.DataConstants.DOCUMENT);

            if (editor == null || document == null) throw new ArgumentException("context");
			ICSharpFile file = solution.GetPsiServices().GetPsiFile<CSharpLanguage>(new DocumentRange(editor.Document, editor.Caret.Offset())) as ICSharpFile;
            if (file == null) return;
            
            ITreeNode element = file.FindNodeAt(new TreeTextRange(new TreeOffset(editor.Caret.Offset())));
            HandleElement(editor, element, editor.Caret.Offset());

        }

        private static void HandleElement(ITextControl editor, ITreeNode element, int offset)
        {
            string stringToInsert = Clipboard.GetText();
            if (string.IsNullOrEmpty(stringToInsert))
            {
                return;
            }

            IDocCommentNode docCommentNode = element as IDocCommentNode;
            if (docCommentNode != null)
            {
                JetBrains.Util.dataStructures.TypedIntrinsics.Int32<DocLine> currentLineNumber =
                    editor.Document.GetCoordsByOffset(editor.Caret.Offset()).Line;
                string currentLine = editor.Document.GetLineText(currentLineNumber);
                int index = currentLine.IndexOf("///", StringComparison.Ordinal);
                if (index < 0)
                {
                    return;
                }
                string prefix = currentLine.Substring(0, index);

                if (ShallEscape(docCommentNode, editor.Caret.Offset()) &&
                    JetBrains.UI.RichText.RichTextBlockToHtml.HtmlEncode(stringToInsert) != stringToInsert &&
                    MessageBox.ShowYesNo("Do you want the text to be escaped?"))
                {
                    stringToInsert = JetBrains.UI.RichText.RichTextBlockToHtml.HtmlEncode(stringToInsert);
                }

                stringToInsert = stringToInsert.Replace("\n", "\n" + prefix + "///");
            }

            ITokenNode token = element as ITokenNode;
            if (token != null)
            {
                if (token.GetTokenType() == CSharpTokenType.STRING_LITERAL &&
                    offset < token.GetTreeTextRange().EndOffset.Offset)
                {
                    string text = token.GetText();
                    if (text.StartsWith("@") && offset > token.GetTreeTextRange().StartOffset.Offset + 1)
                    {
                        stringToInsert = stringToInsert.Replace("\"", "\"\"");
                    }
                    else if (!text.StartsWith("@"))
                    {
                        stringToInsert = stringToInsert.
                            Replace("\\", "\\\\").
                            Replace("\a", "\\a").
                            Replace("\b", "\\b").
                            Replace("\f", "\\f").
                            Replace("\n", "\\n").
                            Replace("\r", "\\r").
                            Replace("\t", "\\t").
                            Replace("\v", "\\v").
                            Replace("\'", "\\'").
                            Replace("\"", "\\\"");
                    }
                }
            }

            editor.Document.InsertText(editor.Caret.Offset(), stringToInsert);
        }

        private static bool ShallEscape(IDocCommentNode node, int offset)
        {
            IDocCommentBlockNode docBlock = node.GetContainingNode<IDocCommentBlockNode>(true);
            if (docBlock == null)
            {
                return false;
            }
            XmlDocLexer lexer = new XmlDocLexer(docBlock);
            lexer.Start();

            bool inCData = false;
            bool insideTag = false;
            while (lexer.TokenType != null)
            {
                if (lexer.TokenType == lexer.XmlTokenType.TAG_START)
                {
                    insideTag = true;
                }
                else if (lexer.TokenType == lexer.XmlTokenType.TAG_END)
                {
                    insideTag = false;
                }
                else if (lexer.TokenType == lexer.XmlTokenType.TAG_START1)
                {
                    insideTag = true;
                }
                else if (lexer.TokenType == lexer.XmlTokenType.TAG_END1)
                {
                    insideTag = false;
                }
                else if (lexer.TokenType == lexer.XmlTokenType.CDATA_START)
                {
                    inCData = true;
                }
                else if (lexer.TokenType == lexer.XmlTokenType.CDATA_END)
                {
                    inCData = false;
                }
                else if (offset >= lexer.TokenLocalRange.StartOffset && offset <= lexer.TokenLocalRange.EndOffset)
                {
                    return !inCData && !insideTag;
                }
                else if (offset < lexer.TokenLocalRange.StartOffset)
                {
                    return false;
                }
                lexer.Advance();
            }

            return false;
        }

        #endregion

        private static bool IsAvailable(IDataContext context)
        {
            ISolution solution = context.GetData(JetBrains.ProjectModel.DataContext.DataConstants.SOLUTION);
			IDocument document = context.GetData(JetBrains.DocumentModel.DataConstants.DOCUMENT);
            IPsiSourceFile file = null;
            if (solution != null && document != null) file = document.GetPsiSourceFile(solution);
            return solution != null && document != null && file != null && file.PrimaryPsiLanguage.Is<CSharpLanguage>() ;
        }
    }
}