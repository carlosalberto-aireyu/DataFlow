using DataFlow.Core.Models;
using DataFlow.UI.ViewModels.Base;
using System;

namespace DataFlow.UI.ViewModels
{
    public class ParametroItemViewModel : ViewModelBase
    {
        // Campos de respaldo para las propiedades
        private int _id;
        private string _parametroKey;
        private string _name;
        private string _parametroValue;
        private string? _description;
        private DateTime _createdAt;
        private DateTime _updatedAt;

        private bool _isEditing;
        private bool _isSelected;

        public ParametroItemViewModel(int id, string parametroKey, string name, string parametroValue, string? description, DateTime createdAt, DateTime updatedAt)
        {
            _id = id;
            _parametroKey = parametroKey ?? throw new ArgumentNullException(nameof(parametroKey));
            _name = name ?? _parametroKey;
            _parametroValue = parametroValue ?? throw new ArgumentNullException(nameof(parametroValue));
            _description = description;
            _createdAt = createdAt;
            _updatedAt = updatedAt;

            _isEditing = false;
            _isSelected = false;
        }

        public ParametroItemViewModel()
        {
            _parametroKey = string.Empty;
            _name = string.Empty;
            _parametroValue = string.Empty;
            _createdAt = DateTime.UtcNow;
            _updatedAt = DateTime.UtcNow;
        }

        public int Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public string ParametroKey
        {
            get => _parametroKey; 
            set => SetProperty(ref _parametroKey, value); 
        }
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public string ParametroValue
        {
            get => _parametroValue;
            set => SetProperty(ref _parametroValue, value);
        }

        public string? Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        public DateTime CreatedAt
        {
            get => _createdAt;
            set
            {
                if (SetProperty(ref _createdAt, value))
                {
                    Raise(nameof(CreatedAtFormatted)); 
                }
            }
        }
        public DateTime UpdatedAt
        {
            get => _updatedAt;
            set
            {
                if (SetProperty(ref _updatedAt, value))
                {
                    Raise(nameof(UpdatedAtFormatted)); 
                }
            }
        }
        public string CreatedAtFormatted => UtcToLocalTimeConverter.ToLocalFormatted(CreatedAt);
        public string UpdatedAtFormatted => UtcToLocalTimeConverter.ToLocalFormatted(UpdatedAt);

        public bool IsEditing
        {
            get => _isEditing;
            set => SetProperty(ref _isEditing, value);
        }
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        public Parametro ToModel()
        {
            return new Parametro
            {
                Id = this.Id,
                ParametroKey = this.ParametroKey,
                Name = this.Name,
                ParametroValue = this.ParametroValue,
                Description = this.Description,
                CreatedAt = this.CreatedAt,
                UpdatedAt = this.UpdatedAt
            };
        }

        public static ParametroItemViewModel FromModel(Parametro parametro)
        {
            if (parametro == null) return new ParametroItemViewModel();
            return new ParametroItemViewModel
            {
                Id = parametro.Id,
                ParametroKey = parametro.ParametroKey,
                Name = parametro.Name,
                ParametroValue = parametro.ParametroValue,
                Description = parametro.Description,
                CreatedAt = parametro.CreatedAt,
                UpdatedAt = parametro.UpdatedAt
            };
        }

        public void UpdateFromModel(Parametro newParametro)
        {
            if (newParametro is null) return;
            Id = newParametro.Id;
            ParametroKey = newParametro.ParametroKey;
            Name = newParametro.Name;
            ParametroValue = newParametro.ParametroValue;
            Description = newParametro.Description;
            CreatedAt = newParametro.CreatedAt;
            UpdatedAt = newParametro.UpdatedAt;
        }
    }
}