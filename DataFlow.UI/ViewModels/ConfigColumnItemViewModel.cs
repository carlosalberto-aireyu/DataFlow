using DataFlow.Core.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataFlow.UI.ViewModels
{
    public class ConfigColumnItemViewModel : INotifyPropertyChanged
    {
        private int _indexcolumn;
        private string? _name;
        private string? _nameDisplay;
        private string? _description;
        private int _dataTypeId;
        private string? _defaultValue;
        private int _columnTypeId;
        private DateTime _createdAt;
        private DateTime _updatedAt;
        private bool _isSelected;
        private ObservableCollection<ColumnRangeItemViewModel>? _ranges;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void Raise(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public int Id { get; set; }
        public int ConfigTemplateId { get; set; }

        public int IndexColumn
        {
            get => _indexcolumn;
            set
            {
                if (_indexcolumn != value)
                {
                    _indexcolumn = value;
                    Raise(nameof(IndexColumn));
                }
            }
        }

        public string? Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    Raise(nameof(Name));
                }
            }
        }

        public string? NameDisplay
        {
            get => _nameDisplay;
            set
            {
                if (_nameDisplay != value)
                {
                    _nameDisplay = value;
                    Raise(nameof(NameDisplay));
                }
            }
        }

        public string? Description
        {
            get => _description;
            set
            {
                if (_description != value)
                {
                    _description = value;
                    Raise(nameof(Description));
                }
            }
        }

        public int DataTypeId
        {
            get => _dataTypeId;
            set
            {
                if (_dataTypeId != value)
                {
                    _dataTypeId = value;
                    Raise(nameof(DataTypeId));
                }
            }
        }
        public DataTypeLookup? DataType { get; set; }
        

        public string? DefaultValue
        {
            get => _defaultValue;
            set
            {
                if (_defaultValue != value)
                {
                    _defaultValue = value;
                    Raise(nameof(DefaultValue));
                }
            }
        }


        public int ColumnTypeId
        {
            get => _columnTypeId;
            set
            {
                if (_columnTypeId != value)
                {
                    _columnTypeId = value;
                    Raise(nameof(ColumnTypeId));
                }
            }
        }
        public ColumnTypeLookup? ColumnType { get; set; }
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

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    Raise(nameof(IsSelected));
                }
            }
        }

        public ObservableCollection<ColumnRangeItemViewModel>? Ranges
        {
            get => _ranges;
            set
            {
                if (_ranges != value)
                {
                    _ranges = value;
                    Raise(nameof(Ranges));
                }
            }
        }

        public string CreatedAtFormatted => UtcToLocalTimeConverter.ToLocalFormatted(CreatedAt);
        public string UpdatedAtFormatted => UtcToLocalTimeConverter.ToLocalFormatted(UpdatedAt);
        public string DisplayName => $"{Name}";

        public ConfigColumnItemViewModel()
        {
            _ranges = new ObservableCollection<ColumnRangeItemViewModel>();
        }

        public ConfigColumnItemViewModel(
            int id,
            int indexcolumn,
            int configTemplateId,
            string? name,
            string? nameDisplay,
            string? description,
            int dataTypeId,
            string? defaultValue,
            int columnTypeId,
            DateTime createdAt,
            DateTime updatedAt)
        {
            Id = id;
            ConfigTemplateId = configTemplateId;
            _indexcolumn = indexcolumn;
            _name = name;
            _nameDisplay = nameDisplay;
            _description = description;
            _dataTypeId = dataTypeId;
            _defaultValue = defaultValue;
            _columnTypeId = columnTypeId;
            _createdAt = createdAt;
            _updatedAt = updatedAt;
            _isSelected = false;
            _ranges = new ObservableCollection<ColumnRangeItemViewModel>();
        }

        public static ConfigColumnItemViewModel FromModel(ConfigColumn model)
        {
            var vm = new ConfigColumnItemViewModel(
                model.Id,
                model.IndexColumn,
                model.ConfigTemplateId,
                model.Name,
                model.NameDisplay,
                model.Description,
                model.DataTypeId,
                model.DefaultValue,
                model.ColumnTypeId,
                model.CreatedAt,
                model.UpdatedAt);

            vm.DataType = model.DataType;
            vm.ColumnType = model.ColumnType;

            if (model.Ranges != null)
            {
                vm.Ranges = new ObservableCollection<ColumnRangeItemViewModel>(
                    model.Ranges.Select(r => ColumnRangeItemViewModel.FromModel(r)));
            }

            return vm;
        }

        public void UpdateFromModel(ConfigColumn model)
        {
            Id = model.Id;
            IndexColumn = model.IndexColumn;
            ConfigTemplateId = model.ConfigTemplateId;
            Name = model.Name;
            NameDisplay = model.NameDisplay;
            Description = model.Description;
            DataTypeId = model.DataTypeId;
            DefaultValue = model.DefaultValue;
            ColumnTypeId = model.ColumnTypeId;
            CreatedAt = model.CreatedAt;
            UpdatedAt = model.UpdatedAt;

            DataType = model.DataType;
            ColumnType = model.ColumnType;

            if (model.Ranges != null)
            {
                Ranges = new ObservableCollection<ColumnRangeItemViewModel>(
                    model.Ranges.Select(r => ColumnRangeItemViewModel.FromModel(r)));
            }
            //Raise(nameof(DataType));
            //Raise(nameof(ColumnType));
        }

        public override string ToString() => DisplayName;
    }

}
