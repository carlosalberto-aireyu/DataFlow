using DataFlow.Core.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace DataFlow.UI.ViewModels
{
    public class ConfigTemplateItemViewModel : INotifyPropertyChanged
    {
        private string? _description;
        private DateTime _createdAt;
        private DateTime _updatedAt;
        private bool _isSelected;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void Raise(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public int Id { get; set; }
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
        public string CreatedAtFormatted => CreatedAt.ToString("dd/MM/yyyy HH:mm");
        public string UpdatedAtFormatted => UpdatedAt.ToString("dd/MM/yyyy HH:mm");
        public string DisplayName => $"{Id} - {Description}";
        public ConfigTemplateItemViewModel()
        {
        }
        public ConfigTemplateItemViewModel(int id, string? description, DateTime createdAt, DateTime updatedAt)
        {
            Id = id;
            _description = description;
            _createdAt = createdAt;
            _updatedAt = updatedAt;
            _isSelected = false;
        }
        public static ConfigTemplateItemViewModel FromModel(ConfigTemplate model)
        {
            return new ConfigTemplateItemViewModel(
                model.Id,
                model.Description,
                model.CreatedAt,
                model.UpdatedAt);
        }
        public void UpdateFromModel(ConfigTemplate model)
        {
            Id = model.Id;
            Description = model.Description;
            CreatedAt = model.CreatedAt;
            UpdatedAt = model.UpdatedAt;
        }
        public ConfigTemplate ToModel()
        {
            return new ConfigTemplate
            {
                Id = this.Id,
                Description = this.Description,
                CreatedAt = this.CreatedAt,
                UpdatedAt = this.UpdatedAt
            };
        }
        public override string ToString() => DisplayName;
    }

}
