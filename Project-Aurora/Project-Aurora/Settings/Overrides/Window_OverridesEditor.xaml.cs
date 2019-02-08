﻿using Aurora.Settings.Layers;
using Aurora.Settings.Overrides.Logic;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace Aurora.Settings.Overrides {
    /// <summary>
    /// Interaction logic for Window_OverridesEditor.xaml
    /// </summary>
    public partial class Window_OverridesEditor : Window {

        public Window_OverridesEditor(Layer layer) {
            // Store the layer and ensure it has an override logic assigned to it.
            Layer = layer;
            if (Layer.OverrideLogic == null)
                Layer.OverrideLogic = new Dictionary<string, IOverrideLogic>();

            // Setup UI and databinding stuff
            InitializeComponent();
            Title = $"Overrides Editor for '{layer.Name}'";
            OverridablePropList.ItemsSource = GetAllOverridableProperties(layer);
            DataContext = this;
        }

        /// <summary>
        /// For the given layer, returns a list of all properties on the handler of that layer that have the OverridableAttribute 
        /// applied (i.e. have been marked overridable for the overrides system).
        /// </summary>
        private List<Tuple<string, string, Type>> GetAllOverridableProperties (Layer layer) {
            return layer.Handler.Properties.GetType().GetProperties() // Get all properties on the layer handler's property list
                .Where(prop => prop.GetCustomAttributes(typeof(LogicOverridableAttribute), true).Length > 0) // Filter to only return the PropertyInfos that have Overridable
                .Select(prop => new Tuple<string, string, Type>( // Return the name and type of these properties.
                    prop.Name, // The actual C# property name
                    ((LogicOverridableAttribute)prop.GetCustomAttributes(typeof(LogicOverridableAttribute), true)[0]).Name, // Get the name specified in the attribute (so it is prettier for the user)
                    Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType // If the property is a nullable type (e.g. bool?), will instead return the non-nullable type (bool)
                ))
                .OrderBy(tup => tup.Item2)
                .ToList();
        }
    }

    /// <summary>
    /// State properties for the Window_OverridesEditor class.
    /// </summary>
    public partial class Window_OverridesEditor : INotifyPropertyChanged {

        // List of all IOverrideLogic types that the user can select
        public Dictionary<string, Type> OverrideTypes { get; } = new Dictionary<string, Type> {
            { "None", null },
            { "Lookup Table", typeof(OverrideLookupTable) }
        };

        // Property change event
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(params string[] affectedProperties) {
            foreach (var prop in affectedProperties)
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        // The layer being edited by the window
        public Layer Layer { get; }

        // The name of the selected property that is being edited
        private Tuple<string, string, Type> _selectedProperty;
        public Tuple<string, string, Type> SelectedProperty {
            get => _selectedProperty;
            set {
                _selectedProperty = value;
                OnPropertyChanged("SelectedProperty", "SelectedLogic", "SelectedLogicType");
            }
        }

        // The override logic for the currently selected property
        public IOverrideLogic SelectedLogic => _selectedProperty == null || !Layer.OverrideLogic.ContainsKey(_selectedProperty.Item1)
            ? null // Return nothing if nothing in the list is selected or there is no logic for this property
            : Layer.OverrideLogic[_selectedProperty.Item1];

        // The type of logic in use by the selected property
        public Type SelectedLogicType {
            get => SelectedLogic?.GetType();

            // When this is set, it means the user has changed the dropdown value
            set {
                // If there is a property selected in the list and the logic type is not set to the same value as it already was
                if (_selectedProperty != null && SelectedLogic?.GetType() != value) {
                    if (value == null) // If the value is null, that means the user selected the "None" option, so remove the override for this property
                        Layer.OverrideLogic.Remove(_selectedProperty.Item1);
                    else // Else if the user selected a non-"None" option, create a new instance of that OverrideLogic and assign it to this property
                        Layer.OverrideLogic[_selectedProperty.Item1] = (IOverrideLogic)Activator.CreateInstance(value, _selectedProperty.Item3);
                    OnPropertyChanged("SelectedLogic", "SelectedLogicType"); // Raise an event to update the control
                }
            }
        }
    }



    /// <summary>
    /// Simple converter to convert a type to it's name (instead of using ToString beacuse that gives the fully qualified name).
    /// </summary>
    public class PrettyTypeNameConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => ((Type)value).Name;
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    /// <summary>
    /// Simple converter to convert a type to a pretty icon used in the list.
    /// </summary>
    public class TypeToIconConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            var imageName = new Dictionary<Type, string> {
                { typeof(bool), "icons8-checked-checkbox-30.png" },
                { typeof(float), "icons8-numbers-30.png" },
                { typeof(int), "icons8-numbers-30.png" },
                { typeof(long), "icons8-numbers-30.png" },
                { typeof(Color), "icons8-paint-palette-30.png" }
            }.TryGetValue((Type)value, out string val) ? val : "icons8-diamonds-30.png";
            return new BitmapImage(new Uri($"/Aurora;component/Resources/{imageName}", UriKind.Relative));
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    /// <summary>
    /// Simple converter that returns true if the given property has a value or false if it is null.
    /// </summary>
    public class HasValueToBoolConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value != null;
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
