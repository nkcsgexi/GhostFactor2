using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Roslyn.Services;
using warnings.refactoring;

namespace warnings.conditions
{
    partial class InlineMethodConditionCheckersList
    {
        private class VariableNamesCollisionChecker : InlineMethodConditionsChecker
        {
            public override ICodeIssueComputer CheckInlineMethodCondition(
                IInlineMethodRefactoring refactoring)
            {
                return new NullCodeIssueComputer();
            }
        }
    }
}
