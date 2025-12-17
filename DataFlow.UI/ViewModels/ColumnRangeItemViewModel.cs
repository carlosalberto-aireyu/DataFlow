using DataFlow.Core.Models;
using System;
using System.ComponentModel;

namespace DataFlow.UI.ViewModels
{
    public class ColumnRangeItemViewModel : INotifyPropertyChanged
    {
        private int _id;
        private int _configColumnId;
        private string? _rFrom;
        private string? _rTo;
        private string? _defaultValue;
        private DateTime _createdAt;
        private DateTime _updatedAt;
        private bool _isNewRow;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void Raise(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public int Id
        {
            get => _id;
            set
            {
                if (_id != value)
                {
                    _id = value;
                    Raise(nameof(Id));
                    Raise(nameof(IsNewRow));
                }
            }
        }

        public int ConfigColumnId
        {
            get => _configColumnId;
            set
            {
                if (_configColumnId != value)
                {
                    _configColumnId = value;
                    Raise(nameof(ConfigColumnId));
                }
            }
        }

        public string? RFrom
        {
            get => _rFrom;
            set
            {
                if (_rFrom != value)
                {
                    _rFrom = value?.ToUpperInvariant();
                    Raise(nameof(RFrom));
                    Raise(nameof(IsValid));
                    Raise(nameof(ValidationMessage));
                }
            }
        }

        public string? RTo
        {
            get => _rTo;
            set
            {
                if (_rTo != value)
                {
                    _rTo = value?.ToUpperInvariant(); 
                    Raise(nameof(RTo));
                    Raise(nameof(IsValid));
                    Raise(nameof(ValidationMessage));
                }
            }
        }

        public string? DefaultValue
        {
            get => _defaultValue;
            set
            {
                if (_defaultValue != value)
                {
                    _defaultValue = value;
                    Raise(nameof(DefaultValue));
                    Raise(nameof(IsValid));
                    Raise(nameof(ValidationMessage));
                }
            }
        }

        public DateTime CreatedAt
        {
            get => _createdAt;
            set
            {
                if (_createdAt != value)
                {
                    _createdAt = value;
                    Raise(nameof(CreatedAt));
                    Raise(nameof(CreatedAtFormatted));
                }
            }
        }

        public DateTime UpdatedAt
        {
            get => _updatedAt;
            set
            {
                if (_updatedAt != value)
                {
                    _updatedAt = value;
                    Raise(nameof(UpdatedAt));
                    Raise(nameof(UpdatedAtFormatted));
                }
            }
        }

        public bool IsNewRow
        {
            get => _isNewRow || _id == 0;
            set
            {
                if (_isNewRow != value)
                {
                    _isNewRow = value;
                    Raise(nameof(IsNewRow));
                }
            }
        }

        
        public bool IsDimensionColumn { get; set; }

        
        public bool IsValid
        {
            get
            {
                
                if (string.IsNullOrWhiteSpace(RFrom) && string.IsNullOrWhiteSpace(RTo))
                    return true;

                
                if (string.IsNullOrWhiteSpace(RFrom) || string.IsNullOrWhiteSpace(RTo))
                    return false;

                
                if (IsDimensionColumn && string.IsNullOrWhiteSpace(DefaultValue))
                    return false;

                return true;
            }
        }

        public string ValidationMessage
        {
            get
            {
                if (string.IsNullOrWhiteSpace(RFrom) && string.IsNullOrWhiteSpace(RTo))
                    return string.Empty;

                if (string.IsNullOrWhiteSpace(RFrom))
                    return "El valor 'Desde' es requerido.";

                if (string.IsNullOrWhiteSpace(RTo))
                    return "El valor 'Hasta' es requerido.";

                
                if (IsDimensionColumn && string.IsNullOrWhiteSpace(DefaultValue))
                    return "Para columnas de tipo Dimensión, el Valor por Defecto es obligatorio.";

                return string.Empty;
            }
        }

        public string CreatedAtFormatted => UtcToLocalTimeConverter.ToLocalFormatted(CreatedAt);
        public string UpdatedAtFormatted => UtcToLocalTimeConverter.ToLocalFormatted(UpdatedAt);
        public string DisplayName => $"Rango {RFrom} - {RTo}";

        public ColumnRangeItemViewModel()
        {
            _isNewRow = true;
        }

        public static ColumnRangeItemViewModel FromModel(ColumnRange model)
        {
            return new ColumnRangeItemViewModel
            {
                Id = model.Id,
                ConfigColumnId = model.ConfigColumnId,
                RFrom = model.RFrom,
                RTo = model.RTo,
                DefaultValue = model.DefaultValue,
                CreatedAt = model.CreatedAt,
                UpdatedAt = model.UpdatedAt,
                IsNewRow = false
            };
        }

        public void UpdateFromModel(ColumnRange model)
        {
            Id = model.Id;
            ConfigColumnId = model.ConfigColumnId;
            RFrom = model.RFrom;
            RTo = model.RTo;
            DefaultValue = model.DefaultValue;
            CreatedAt = model.CreatedAt;
            UpdatedAt = model.UpdatedAt;
            IsNewRow = false;
        }

        public override string ToString() => DisplayName;
    }
}