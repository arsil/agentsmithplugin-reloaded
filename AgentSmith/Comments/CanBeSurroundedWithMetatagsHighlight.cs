using AgentSmith.Comments;

using JetBrains.DocumentModel;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;

[assembly: RegisterConfigurableSeverity(
    CanBeSurroundedWithMetatagsHighlight.SEVERITY_ID,
    null,
    HighlightingGroupIds.CodeSmell,
    "Identifiers in XML documentation should be surrounded with meta-tags",
    "Identifiers in XML documentation should be surrounded with meta-tags",
    Severity.SUGGESTION,
    false)]

namespace AgentSmith.Comments
{
    [ConfigurableSeverityHighlighting(SEVERITY_ID, CSharpLanguage.Name)]
    public class CanBeSurroundedWithMetatagsHighlight : IHighlighting
    {
        public const string SEVERITY_ID = "WordCanBeSurroundedWithMetaTags";

        private const string SUGGESTION_TEXT =
            "Word '{0}' appears to be an identifier and can be surrounded with meta-tag.";

        private readonly IClassMemberDeclaration _declaration;
        private readonly ISolution _solution;
        private readonly string _word;

        private DocumentRange _range;

        public CanBeSurroundedWithMetatagsHighlight(
            string word, DocumentRange highlightingRange,
            IClassMemberDeclaration declaration, ISolution solution)
        {
            _range = highlightingRange;
            _solution = solution;
            _declaration = declaration;
            _word = word;
        }

        public IClassMemberDeclaration Declaration
        {
            get { return _declaration; }
        }

        public ISolution Solution
        {
            get { return _solution; }
        }

        public string Word
        {
            get { return _word; }
        }

        public DocumentRange DocumentRange
        {
            get { return _range; }
        }

        public bool IsValid()
        {
            return true;
        }

        public string ToolTip { get { return string.Format(SUGGESTION_TEXT, _word); } }

        public string ErrorStripeToolTip { get { return ToolTip; } }

        public int NavigationOffsetPatch { get { return 0; } }
    }
}