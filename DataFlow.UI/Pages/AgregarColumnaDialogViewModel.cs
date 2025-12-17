using DataFlow.Core.Models;
using DataFlow.UI.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace DataFlow.UI.Pages
{
    public class AgregarColumnaDialogViewModel : INotifyPropertyChanged
    {
        private readonly ILookupService _lookupService;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void Raise([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public AgregarColumnaDialogViewModel(ILookupService lookupService)
        {
            _lookupService = lookupService
                ?? throw new ArgumentNullException(nameof(lookupService));
            LoadLookupsAsync().GetAwaiter().GetResult();
            SelectedColumnType = ColumnTypes.FirstOrDefault();
            SelectedDataType = DataTypes.FirstOrDefault();
        }


        private int _indexColumn;
        public int IndexColumn
        {
            get => _indexColumn;
            set { _indexColumn = value; Raise(); }
        }

        private string? _columnName;
        public string? ColumnName
        {
            get => _columnName;
            set
            {
                var old = _columnName;
                _columnName = value;
                Raise();

                if (string.IsNullOrWhiteSpace(DisplayName) || DisplayName == old)
                    DisplayName = value;
            }
        }

        private string? _displayName;
        public string? DisplayName
        {
            get => _displayName;
            set { _displayName = value; Raise(); }
        }

        private string? _description;
        public string? Description
        {
            get => _description;
            set { _description = value; Raise(); }
        }

        private string? _defaultValue;
        public string? DefaultValue
        {
            get => _defaultValue;
            set { _defaultValue = value; Raise(); }
        }


        public ObservableCollection<DataTypeLookup> DataTypes { get; } = new();
        public ObservableCollection<ColumnTypeLookup> ColumnTypes { get; } = new();

        private DataTypeLookup? _selectedDataType;
        public DataTypeLookup? SelectedDataType
        {
            get => _selectedDataType;
            set { _selectedDataType = value; Raise(); }
        }

        private ColumnTypeLookup? _selectedColumnType;
        public ColumnTypeLookup? SelectedColumnType
        {
            get => _selectedColumnType;
            set { _selectedColumnType = value; Raise(); }
        }


        public async Task LoadLookupsAsync()
        {
            var dataTypes = await _lookupService.GetDataTypesAsync();
            DataTypes.Clear();
            foreach (var dt in dataTypes)
                DataTypes.Add(dt);

            var columnTypes = await _lookupService.GetColumnTypesAsync();
            ColumnTypes.Clear();
            foreach (var ct in columnTypes)
                ColumnTypes.Add(ct);
        }
    }
}
