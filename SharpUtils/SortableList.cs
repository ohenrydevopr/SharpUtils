using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;

namespace SharpUtils
{
    public class SortableList<T> : BindingList<T>
    {
        // Keep the original list
        List<T> _original;
        // Keep the sort property
        PropertyDescriptor? _property;
        // Curretn Sort direction
        ListSortDirection _direction = ListSortDirection.Ascending;
        // Cache sort expressions
        Dictionary<string, Func<List<T>, IEnumerable<T>>> _cache = new();

        // Constructors
        public SortableList() { _original = new(); }
        public SortableList(List<T> original) { _original = original; Populate(this, _original); }
        public SortableList(IEnumerable<T> original) { _original = original.ToList(); Populate(this, _original); }


        // Methods
        private void Reset(List<T> items)
        {
            ClearItems();
            for (int i = 0; i < items.Count; i++) { InsertItem(i, items[i]); }
        }
        private void CreateSortExpressioCache(PropertyDescriptor prop, string orderByMethodName, string sortExpressionCacheKey)
        {
            ParameterExpression sourceParameter = Expression.Parameter(typeof(List<T>), "source");
            ParameterExpression lambdaParameter = Expression.Parameter(typeof(T), "lambdaParameter");
            PropertyInfo accessMember = typeof(T).GetProperty(prop.Name)!;
            LambdaExpression lambdaSelector = Expression.Lambda(Expression.MakeMemberAccess(lambdaParameter, accessMember), lambdaParameter);
            MethodInfo orderByMethod = typeof(Enumerable).GetMethods().Where(m => m.Name == orderByMethodName && m.GetParameters().Length == 2).Single().MakeGenericMethod(typeof(T), prop.PropertyType);
            Expression[] expression = new Expression[] { sourceParameter, lambdaSelector };
            Expression<Func<List<T>, IEnumerable<T>>> sortExpression = Expression.Lambda<Func<List<T>, IEnumerable<T>>>(Expression.Call(orderByMethod, expression), sourceParameter);
            _cache.Add(sortExpressionCacheKey, sortExpression.Compile());
        }

        // Actions
        // Resets original list
        Action<SortableList<T>, List<T>> Populate = (a, b) => a.Reset(b);

        // Methods Overrides
        protected override void RemoveSortCore() { Reset(_original); }
        protected override void OnListChanged(ListChangedEventArgs e) { _original = Items.ToList(); }
        protected override void ApplySortCore(PropertyDescriptor prop, ListSortDirection direction)
        {
            _property = prop;
            string orderByMethodName = _direction == ListSortDirection.Ascending ? "OrderBy" : "OrderByDescending";
            string sortExpressionCacheKey = typeof(T).GUID + prop.Name + orderByMethodName;
            if (!_cache.ContainsKey(sortExpressionCacheKey)) { CreateSortExpressioCache(prop, orderByMethodName, sortExpressionCacheKey); }

            Reset(_cache[sortExpressionCacheKey](_original).ToList());
            ResetBindings();
            _direction = _direction == ListSortDirection.Ascending ? ListSortDirection.Descending : ListSortDirection.Ascending;
        }

        // Properties Overrides
        protected override bool SupportsSortingCore { get { return true; } }
        protected override ListSortDirection SortDirectionCore { get { return _direction; } }
        protected override PropertyDescriptor? SortPropertyCore { get { return _property; } }
    }
}