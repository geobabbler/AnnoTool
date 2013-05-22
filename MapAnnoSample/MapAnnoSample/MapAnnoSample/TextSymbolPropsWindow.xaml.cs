using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace Zekiah.Samples
{
    public partial class TextSymbolPropsWindow : ChildWindow
    {
        //private string _textFont = "Arial";
        //private string _annotation = "Text Preview";
        //private double _fontSize = 10;

        ICollection<Typeface> typefaces = System.Windows.Media.Fonts.SystemTypefaces;


        public bool EditMode { get; set; }
        public string TextFont { 
            get
            {
                ComboBoxItem itm = this.cboFonts.SelectedItem as ComboBoxItem;
                return itm.Content.ToString();
            }
            set
            {
                foreach (object o in this.cboFonts.Items)
                {
                    ComboBoxItem itm = o as ComboBoxItem;
                    if (itm.Content.ToString().ToLower() == value.ToLower())
                    {
                        this.cboFonts.SelectedItem = itm;
                        this.textBox1.FontFamily = new FontFamily(value);
                    }
                }
                //this.textBox1.Text = value;
            }
        }
        public string Annotation { 
            get
            {
                return this.textBox1.Text;
            }
            set
            {
                this.textBox1.Text = value;
            }
        }
        public double TextFontSize 
        { 
            get
            {
                return this.fontSizePicker.Value;
            }
            set
            {
                this.fontSizePicker.Value = value;
            }
        }

        public TextSymbolPropsWindow()
        {
            InitializeComponent();
            
            //foreach (System.Windows.Media.Typeface face in typefaces)
            //{
            //    System.Windows.Media.GlyphTypeface g;
            //    ListBoxItem listBoxItem = new ListBoxItem();
                
            //    face.TryGetGlyphTypeface(out g);
            //    FontSource fs = new FontSource(g);
            //    var fontname = g.FontFileName;
            //    listBoxItem.Content = fontname.ToString();
            //    cboFonts.Items.Add(listBoxItem);
            //}
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            this.Annotation = this.textBox1.Text;
            //this.TextFont = 
            this.DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private void cboFonts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var itm = this.cboFonts.SelectedItem as ComboBoxItem;
                this.TextFont = itm.Content.ToString();
                this.textBox1.FontFamily = new FontFamily(itm.Content.ToString());
            }
            catch { }
        }

        private void numericUpDown1_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                var picker = sender as NumericUpDown;
                this.textBox1.FontSize = picker.Value;
                this.TextFontSize = this.fontSizePicker.Value;
            }
            catch { }
        }

        private void ChildWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.textBox1.FontSize = this.fontSizePicker.Value;
            this.TextFontSize = this.fontSizePicker.Value;
        }
    }
}

