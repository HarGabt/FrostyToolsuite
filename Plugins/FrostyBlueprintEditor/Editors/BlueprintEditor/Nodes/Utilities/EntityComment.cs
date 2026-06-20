using System;
using System.Drawing;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using BlueprintEditorPlugin.Editors.GraphEditor.LayoutManager.IO;
using BlueprintEditorPlugin.Models.Entities;
using BlueprintEditorPlugin.Models.Nodes;
using BlueprintEditorPlugin.Models.Nodes.Utilities;
using Frosty.Core.Controls;
using FrostySdk.Ebx;
using FrostySdk.IO;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.Utilities
{
    public class EntityComment : BaseComment, IObjectContainer
    {
        private SolidColorBrush _headerColor;
        public SolidColorBrush HeaderColor
        {
            get => _headerColor;
            set
            {
                _headerColor = value;
                NotifyPropertyChanged(nameof(HeaderColor));
            }
        }

        public object Object { get; }

        public EntityComment(string header, Color color)
        {
            Header = header;
            HeaderColor = new SolidColorBrush(color);
            Object = new EditCommentArgs(this);
        }

        public EntityComment(string header)
        {
            Header = header;
            HeaderColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3E7F4B"));
            Object = new EditCommentArgs(this);
        }
        
        public EntityComment()
        {
            Header = "Header";
            HeaderColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3E7F4B"));
            Size = new System.Windows.Size(8, 8);
            Object = new EditCommentArgs(this);
        }

        public override bool Load(LayoutReader reader)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Header = reader.ReadNullTerminatedString();
                HeaderColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString(reader.ReadNullTerminatedString()));
                Location = reader.ReadPoint();
                double width = reader.ReadDouble();
                double height = reader.ReadDouble();
                CommentSize = new System.Windows.Size(width, height);
            });
            return true;
        }

        /// <summary>
        /// FORMAT STRUCTURE:
        /// NullTerminatedString - Header
        /// Point - Location
        /// Double - Width
        /// Double - Height
        /// </summary>
        /// <param name="writer"></param>
        public override void Save(LayoutWriter writer)
        {
            writer.WriteNullTerminatedString(Header);
            writer.WriteNullTerminatedString(HeaderColor.Color.ToString());
            writer.Write(Location);
            writer.Write(CommentSize.Width);
            writer.Write(CommentSize.Height);
        }

        
        public void OnObjectModified(object sender, ItemModifiedEventArgs args)
        {
            EditCommentArgs editComment = (EditCommentArgs)Object;
            Header = editComment.Header;
            HeaderColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString(editComment.Color));
        }
    }

    public class EditCommentArgs
    {
        public string Header { get; set; }
        public string Color { get; set; }

        public EditCommentArgs(EntityComment comment)
        {
            Header = comment.Header;
            Color = comment.HeaderColor.Color.ToString();
        }

        public EditCommentArgs()
        {
        }
    }
}