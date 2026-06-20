using System.ComponentModel;
using System.Windows;
using BlueprintEditorPlugin.Editors.GraphEditor.LayoutManager.IO;
using BlueprintEditorPlugin.Editors.GraphEditor.NodeWrangler;
using FrostySdk.IO;

namespace BlueprintEditorPlugin.Models.Nodes.Utilities
{
    public abstract class BaseComment : ITransient
    {
        #region Vertex data

        private string _header;
        public string Header
        {
            get => _header;
            set
            {
                _header = value;
                NotifyPropertyChanged(nameof(Header));
            }
        }

        private Point _location;
        public Point Location
        {
            get => _location;
            set
            {
                _location = value;
                NotifyPropertyChanged(nameof(Location));
            }
        }
        
        public Size Size { get; set; }

        private Size _size;
        
        /// <summary>
        /// Use this for setting the size of comments, sorry!
        /// </summary>
        public Size CommentSize
        {
            get => _size;
            set
            {
                _size = value;
                NotifyPropertyChanged(nameof(CommentSize));
            }
        }

        private bool _selected;

        public bool IsSelected
        {
            get => _selected;
            set
            {
                _selected = value;
                NotifyPropertyChanged(nameof(IsSelected));
            }
        }

        #endregion
        
        public INodeWrangler NodeWrangler { get; }
        
        public virtual bool IsValid()
        {
            return true;
        }

        public virtual void OnCreation()
        {
        }

        public void OnDestruction()
        {
        }

        public abstract bool Load(LayoutReader reader);

        public abstract void Save(LayoutWriter writer);

        #region Property changing

        public event PropertyChangedEventHandler PropertyChanged;
        
        protected void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        public override string ToString()
        {
            return Header ?? base.ToString();
        }
    }
}