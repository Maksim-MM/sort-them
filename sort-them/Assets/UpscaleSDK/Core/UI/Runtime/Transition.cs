using System;
using UnityEngine;

namespace UpscaleSDK.Core.Ui.Runtime
{
    /// <summary>
    /// Represents a transition class.
    /// </summary>
    [Serializable]
    public class Transition
    {
        [SerializeField] private UIElement _origin;
        [SerializeField] private UIElement _north;
        [SerializeField] private UIElement _south;
        [SerializeField] private UIElement _west;
        [SerializeField] private UIElement _east;
        
        /// <summary>
        /// Gets or sets the origin.
        /// </summary>
        public UIElement Origin => _origin;
        /// <summary>
        /// Gets or sets the north.
        /// </summary>
        public UIElement North => _north;
        /// <summary>
        /// Gets or sets the south.
        /// </summary>
        public UIElement South => _south;
        /// <summary>
        /// Gets or sets the west.
        /// </summary>
        public UIElement West => _west;
        /// <summary>
        /// Gets or sets the east.
        /// </summary>
        public UIElement East => _east;

        public Transition(UIElement origin, UIElement north, UIElement south, UIElement west, UIElement east)
        {
            _origin = origin;
            _north = north;
            _south = south;
            _west = west;
            _east = east;
        }
    }
}