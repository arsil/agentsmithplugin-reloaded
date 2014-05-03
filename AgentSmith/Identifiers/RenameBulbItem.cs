using System.Collections.Generic;

using JetBrains.Application;
using JetBrains.Application.DataContext;
using JetBrains.DataFlow;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Intentions.Extensibility;
using JetBrains.ReSharper.Psi.Services;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Refactorings.Function2Property;
using JetBrains.ReSharper.Refactorings.Rename;
using JetBrains.TextControl;

namespace AgentSmith.Identifiers
{
    internal class RenameBulbItem : IBulbAction
    {
        private readonly IDeclaration _declaration;

        private readonly string _targetName;

        public RenameBulbItem(IDeclaration declaration, string targetName = null)
        {
            _declaration = declaration;
            _targetName = targetName;
        }

        #region IBulbItem Members

        public void Execute(ISolution solution, ITextControl textControl)
        {

            IList<IDataRule> provider =
                DataRules.AddRule(
                    "ManualRenameRefactoringItem",
                    JetBrains.ReSharper.Psi.Services.DataConstants.DECLARED_ELEMENTS,
                    DataConstantsEx.ToDeclaredElementsDataConstant(this._declaration.DeclaredElement)
                ).AddRule(
                    "ManualRenameRefactoringItem",
                    JetBrains.TextControl.DataContext.DataConstants.TEXT_CONTROL,
                    textControl
                ).AddRule(
                    "ManualRenameRefactoringItem",
                    JetBrains.ProjectModel.DataContext.DataConstants.SOLUTION,
                    solution);
            
            /*
            if (Shell.Instance.IsTestShell)
                provider.AddRule(
                    "ManualRenameRefactoringItem",
                    RenameWorkflow.RenameDataProvider,
                    new RenameTestDataProvider("TestName", false, false)
                );
            */
            if (_targetName != null)
            provider.AddRule(
                "ManualRenameRefactoringItem",
                RenameWorkflow.RenameDataProvider, new SimpleRenameDataProvider(_targetName));

            Lifetimes.Using(
                (lifetime =>
                    RenameRefactoringService.Instance.ExcuteRename(
                        JetBrains.ActionManagement.ShellComponentsEx.ActionManager(
                            Shell.Instance.Components).DataContexts.CreateWithDataRules(lifetime, provider))));
        }

        public string Text
        {
            get
            {
                if (_targetName != null)
                {
                    return string.Format("Rename to {0}", _targetName);
                }
                return "Rename...";
            }
        }

        #endregion
    }
}