using System;
using System.Collections.Generic;
using System.Linq;

namespace Vrm.Util
{
    public class PredicateBuilder<T>
    {
        private class ConditionEntry
        {
            public Guid Id { get; } = Guid.NewGuid();
            public Predicate<T> Condition { get; set; }
            public LogicalOperator Operator { get; set; }
            public bool Negated { get; set; }
        }

        private enum LogicalOperator
        {
            And,
            Or
        }

        private readonly List<ConditionEntry> _conditions = new List<ConditionEntry>();

        public int ConditionCount => _conditions.Count;

        public Guid And(Predicate<T> predicate)
        {
            return Add(predicate, LogicalOperator.And);
        }

        public Guid Or(Predicate<T> predicate)
        {
            return Add(predicate, LogicalOperator.Or);
        }

        public bool Not(Guid id)
        {
            var cond = _conditions.FirstOrDefault(x => x.Id == id);
            if (cond == null) return false;
            cond.Negated = !cond.Negated;
            return true;
        }

        public bool Remove(Guid id)
        {
            return _conditions.RemoveAll(x => x.Id == id) > 0;
        }

        public bool Remove(Predicate<T> pred)
        {
            return _conditions.RemoveAll(x => x.Condition == pred) > 0;
        }

        public void Clear()
        {
            _conditions.Clear();
        }

        public Predicate<T> Build()
        {
            if (_conditions.Count == 0)
                return _ => true;

            Predicate<T> result = x =>
            {
                bool current = true;
                bool first = true;

                foreach (var entry in _conditions)
                {
                    bool eval = entry.Condition(x);
                    if (entry.Negated)
                        eval = !eval;

                    if (first)
                    {
                        current = eval;
                        first = false;
                        continue;
                    }

                    if (entry.Operator == LogicalOperator.And)
                        current = current && eval;
                    else
                        current = current || eval;
                }

                return current;
            };

            return result;
        }

        private Guid Add(Predicate<T> predicate, LogicalOperator op)
        {
            var entry = new ConditionEntry
            {
                Condition = predicate,
                Operator = op
            };
            _conditions.Add(entry);
            return entry.Id;
        }
    }
}