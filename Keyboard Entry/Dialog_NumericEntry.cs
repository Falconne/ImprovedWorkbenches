using System;
using Verse;

namespace ImprovedWorkbenches
{
    public class Dialog_NumericEntry : Dialog_Rename
    {
        private readonly Predicate<int> _validator;
        private readonly Action<int> _setter;

        public Dialog_NumericEntry(Predicate<int> validator, Action<int> setter)
        {
            _validator = validator;
            _setter = setter;
        }

        protected override void SetName(string name)
        {
            if (!int.TryParse(name, out var result))
                return;

            if (result < 0)
                return;

            if (!_validator(result))
                return;

            _setter(result);
        }
    }
}