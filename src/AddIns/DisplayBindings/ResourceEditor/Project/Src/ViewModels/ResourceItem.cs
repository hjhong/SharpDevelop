﻿// Copyright (c) 2014 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.ComponentModel;
using System.Drawing;
using System.Resources;
using System.Windows;
using System.Windows.Forms;
using ICSharpCode.SharpDevelop;

namespace ResourceEditor.ViewModels
{
	/// <summary>
	/// Defines the type of resource item supported by editor.
	/// </summary>
	public enum ResourceItemEditorType
	{
		Unknown,
		String,
		Boolean,
		Bitmap,
		Icon,
		Cursor,
		Binary
	}
	
	public class ResourceItem : DependencyObject
	{
		ResourceItemEditorType resourceType;
		ResourceEditorViewModel resourceEditor;
		string nameBeforeEditing;
		
		public ResourceItem(ResourceEditorViewModel resourceEditor, string name, object resourceValue)
		{
			this.resourceEditor = resourceEditor;
			this.Name = name;
			this.ResourceValue = resourceValue;
			this.resourceType = GetResourceTypeFromValue(resourceValue);
		}
		
		public ResourceItem(ResourceEditorViewModel resourceEditor, string name, object resourceValue, string comment)
		{
			this.resourceEditor = resourceEditor;
			this.Name = name;
			this.ResourceValue = resourceValue;
			this.resourceType = GetResourceTypeFromValue(resourceValue);
			this.Comment = comment;
		}

		public static readonly DependencyProperty NameProperty =
			DependencyProperty.Register("Name", typeof(string), typeof(ResourceItem),
				new FrameworkPropertyMetadata());
		
		public string Name {
			get { return (string)GetValue(NameProperty); }
			set { SetValue(NameProperty, value); }
		}
		
		public static readonly DependencyProperty ResourceValueProperty =
			DependencyProperty.Register("ResourceValue", typeof(object), typeof(ResourceItem),
				new FrameworkPropertyMetadata());
		
		public object ResourceValue {
			get { return (object)GetValue(ResourceValueProperty); }
			set { SetValue(ResourceValueProperty, value); }
		}
		
		public string DisplayedResourceType {
			get {
				return ResourceValue == null ? "(Nothing/null)" : ResourceValue.GetType().FullName;
			}
		}
		
		public ResourceItemEditorType ResourceType {
			get {
				return resourceType;
			}
		}
		
		public static readonly DependencyProperty IsEditingProperty =
			DependencyProperty.Register("IsEditing", typeof(bool), typeof(ResourceItem),
				new FrameworkPropertyMetadata());
		
		public bool IsEditing {
			get { return (bool)GetValue(IsEditingProperty); }
			set { SetValue(IsEditingProperty, value); }
		}
		
		protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnPropertyChanged(e);
	
			if (e.Property == IsEditingProperty) {
				bool previouslyEditing = (bool)e.OldValue;
				bool isEditing = (bool)e.NewValue;
				if (!previouslyEditing && isEditing) {
					// Save initial name to compare it later on cancellation
					nameBeforeEditing = Name;
				} else if (previouslyEditing && !isEditing) {
					// Make dirty, if name has changed after finishing edit
					if (nameBeforeEditing != Name) {
						// Check if new name is valid
						if (!String.IsNullOrEmpty(Name) && !resourceEditor.ContainsResourceName(Name)) {
							resourceEditor.MakeDirty();
						} else {
							// New name was not valid, revert it to the value before editing
							Name = nameBeforeEditing;
						}
					}
				}
			} else {
				resourceEditor.MakeDirty();
			}
		}
		
		ResourceItemEditorType GetResourceTypeFromValue(object val)
		{
			if (this.ResourceValue == null) {
				return ResourceItemEditorType.Unknown;
			}
			switch (this.ResourceValue.GetType().ToString()) {
				case "System.String":
					return ResourceItemEditorType.String;
				case "System.Drawing.Bitmap":
					return ResourceItemEditorType.Bitmap;
				case "System.Drawing.Icon":
					return ResourceItemEditorType.Icon;
				case "System.Windows.Forms.Cursor":
					return ResourceItemEditorType.Cursor;
				case "System.Byte[]":
					return ResourceItemEditorType.Binary;
				case "System.Boolean":
					return ResourceItemEditorType.Boolean;
				default:
					return ResourceItemEditorType.Unknown;
			}
		}
		
		public string Content {
			get {
				return ToString();
			}
		}
		
		public static readonly DependencyProperty CommentProperty =
			DependencyProperty.Register("Comment", typeof(string), typeof(ResourceItem),
				new FrameworkPropertyMetadata());
		
		public string Comment {
			get { return (string)GetValue(CommentProperty); }
			set { SetValue(CommentProperty, value); }
		}

		public override string ToString()
		{
			if (ResourceValue == null) {
				return "(Nothing/null)";
			}
			
			string type = ResourceValue.GetType().FullName;
			string tmp = String.Empty;
			
			switch (type) {
				case "System.String":
					tmp = ResourceValue.ToString();
					break;
				case "System.Byte[]":
					tmp = "[Size = " + ((byte[])ResourceValue).Length + "]";
					break;
				case "System.Drawing.Bitmap":
					Bitmap bmp = ResourceValue as Bitmap;
					tmp = "[Width = " + bmp.Size.Width + ", Height = " + bmp.Size.Height + "]";
					break;
				case "System.Drawing.Icon":
					Icon icon = ResourceValue as Icon;
					tmp = "[Width = " + icon.Size.Width + ", Height = " + icon.Size.Height + "]";
					break;
				case "System.Windows.Forms.Cursor":
					Cursor c = ResourceValue as Cursor;
					tmp = "[Width = " + c.Size.Width + ", Height = " + c.Size.Height + "]";
					break;
				case "System.Boolean":
					tmp = ResourceValue.ToString();
					break;
				default:
					tmp = ResourceValue.ToString();
					break;
			}
			return tmp;
		}
		
		public ResXDataNode ToResXDataNode(Func<Type, string> typeNameConverter = null)
		{
			var node = new ResXDataNode(Name, ResourceValue, typeNameConverter) {
				Comment = Comment
			};
			return node;
		}
		
		public bool UpdateFromFile()
		{
			var fileDialog = new Microsoft.Win32.OpenFileDialog();
			fileDialog.AddExtension = true;
			fileDialog.Filter = "All files (*.*)|*.*";
			fileDialog.CheckFileExists = true;
			
			if (fileDialog.ShowDialog().Value) {
				object newValue = null;
				switch (resourceType) {
					case ResourceItemEditorType.Bitmap:
						try {
							newValue = new Bitmap(fileDialog.FileName);
						} catch {
							SD.MessageService.ShowWarning("Can't load bitmap file.");
							return false;
						}
						break;
				}
					
				if (newValue != null) {
					ResourceValue = newValue;
					return true;
				}
			}
			
			return false;
		}
	}
}