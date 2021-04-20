using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Csud.Interfaces;

namespace Csud.Base
{
    public class ModelBase
    {
        public T Clone<T>()
        {
            return (T)MemberwiseClone();
        }

        public object Key()
        {
            var properties = GetType().GetProperties();
            var keyProperty = properties.Single(p => p.GetCustomAttributes(typeof(KeyAttribute), false).Any());
            return keyProperty.GetValue(this);
        }

        protected Dictionary<string, object> _currentValues = new Dictionary<string, object>();
        protected Dictionary<string, object> _prevValues = new Dictionary<string, object>();
        private bool _trackChanges = true;

        protected void SetProperty<T>(string propertyName, T propertyValue)
        {
            if (_trackChanges)
            {
                // Если значение свойства уже было установлено
                if (_currentValues.TryGetValue(propertyName, out var val))
                {
                    var currentValue = (T)val;
                    // Если оба значения одинаковы либо null, то не сохраняем историю
                    if (ReferenceEquals(currentValue, propertyValue))
                        return;

                    if (ReferenceEquals(currentValue, null) || !currentValue.Equals(propertyValue))
                        _prevValues[propertyName] = currentValue;
                }
                else
                {
                    _prevValues[propertyName] = null;
                }
            }
            _currentValues[propertyName] = propertyValue;
        }

        public Dictionary<string, object> GetProperties()
        {
            return new Dictionary<string, object>();
        }

        protected T GetProperty<T>(string propertyName)
        {
            if (_currentValues.TryGetValue(propertyName, out var propertyValue))
                return (T)propertyValue;
            return default(T);
        }

        public object this[string propertyName]
        {
            get => GetProperty<object>(propertyName);
            set
            {
                var propertyInfo = GetType().GetProperty(propertyName);
                if (propertyInfo == null)
                {

                    propertyName = propertyName.ToTitleCase();
                    propertyInfo = GetType().GetProperty(propertyName);
                }
                    
                if (propertyInfo != null)
                {
                    var typedValue = value;
                    typedValue = value.Convert(propertyInfo.PropertyType);
                    propertyInfo.SetValue(this, typedValue);
                }
       
            }
        }

        public virtual IEnumerable<KeyValuePair<string, object>> GetPropertyValues()
        {
            return _currentValues;
        }

        public virtual IEnumerable<KeyValuePair<string, object>> GetPrevPropertyValues()
        {
            return _prevValues;
        }

        public bool IsPropertyValueChanged(string propertyName)
        {
            return _prevValues.ContainsKey(propertyName);
        }

        #region ITrackPropertyChanges

        public object GetPrevPropertyValue(string propertyName)
        {
            return _prevValues.GetValueOrDefault(propertyName);
        }

        public virtual IEnumerable<KeyValuePair<string, object>> GetModifiedPropertyValues()
        {
            return _currentValues.Where(x => _prevValues.ContainsKey(x.Key));
        }

        public virtual IEnumerable<KeyValuePair<string, object>> GetHistoryPropertyValues()
        {
            return _prevValues.Concat(_currentValues.Where(x => !_prevValues.ContainsKey(x.Key)));
        }

        public virtual IEnumerable<KeyValuePair<string, object>> GetCurrentPropertyValues()
        {
            return _currentValues;
        }

        public void EnableTracking()
        {
            _trackChanges = true;
        }

        public void DisableTracking()
        {
            _trackChanges = false;
            _prevValues.Clear();
        }

        #endregion
    }

  
}
