using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using Microsoft.Win32;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Color = System.Drawing.Color;

namespace TFShandler
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public bool _imagelodaded = false;
        public Bitmap _imagenfinal;
        public Bitmap _imagenimport;
        public class TFSimage
        {
            public int _posX ; //uint16
            public int _posY ; //uint16
            public int[,] _idata; //uint8 palette index

            public TFSimage()
            {
                _posX = 0; 
                _posY = 0; 
                _idata= new int[128, 128];
            }
        }

        public static string appTitle = "TFS Handler v1.2";
        public int _tfsimageWidth = 0;  //uint16
        public int _tfsimageHeight = 0; //uint16
        public int _tfsimageWidthreal = 0;  //uint16
        public int _tfsimageHeightreal = 0; //uint16
        public int _tfsimagePaletteCount = 0; //uint16
        public int _tfsimagePadding = 0; //uint16
        public Color[,] _tfsPalette = new Color[5, 256];
        public int[,] _tfsPalettevalue = new int[5, 256];
        public List<TFSimage> _tfsImages = new List<TFSimage>();
        

        public MainWindow()
        {
            InitializeComponent();
            this.Title = appTitle;
        }

        public void Load_Image(string _fileName)
        {
            _imagelodaded = false;
            _tfsImages.Clear();
            Combobox_palletesel.Items.Clear();
            using (FileStream fs = new FileStream(_fileName, FileMode.Open, FileAccess.Read))
            {
                using (BinaryReader br = new BinaryReader(fs, new ASCIIEncoding()))
                {
                    byte[] _reader16;
                    byte _reader8;

                    //reading initial values

                    _reader16 = br.ReadBytes(2);
                    _tfsimageWidth = BitConverter.ToUInt16(_reader16);
                    _reader16 = br.ReadBytes(2);
                    _tfsimageHeight = BitConverter.ToUInt16(_reader16);
                    _reader16 = br.ReadBytes(2);
                    _tfsimagePaletteCount = BitConverter.ToUInt16(_reader16);
                    _reader16 = br.ReadBytes(2);
                    _tfsimagePadding = BitConverter.ToUInt16(_reader16);

                    // reading Palettes

                    for (int i = 0; i < _tfsimagePaletteCount; i++)
                    {
                        for (int j = 0; j < 256; j++)
                        {
                            _reader16 = br.ReadBytes(2);
                            int _ctemp = BitConverter.ToUInt16(_reader16);
                            int _cAlpha = 255; //((_ctemp)%2)*255; transparency in tfs doesn't seem to work, just read alpha as true;
                            int _cRed = (((_ctemp >> 0) & 0x1F) * 255 + 15) / 31; //red
                            int _cGreen = (((_ctemp >> 5) & 0x1F) * 255 + 15) / 31;  //green
                            int _cBlue = (((_ctemp >> 10) & 0x1F) * 255 + 15) / 31;  //blue
                            _tfsPalette[i, j] = Color.FromArgb(_cAlpha, _cRed, _cGreen, _cBlue);
                            _tfsPalettevalue[i, j] = _ctemp;
                        }
                        Combobox_palletesel.Items.Add(i);
                    }

                    //reading images


                    while (br.BaseStream.Position < br.BaseStream.Length)
                    {
                        int _temposX = 0;
                        int _temposY = 0;
                        int[,] _tdata = new int[128, 128];
                        _reader16 = br.ReadBytes(2);
                        _temposX = BitConverter.ToUInt16(_reader16);
                        _reader16 = br.ReadBytes(2);
                        _temposY = BitConverter.ToUInt16(_reader16);
                        for (int i = 0; i < 128; i++)
                        {
                            for (int j = 0; j < 128; j++)
                            {
                                _reader8 = br.ReadByte();
                                _tdata[j, i] = _reader8;
                            }
                        }
                        _tfsImages.Add(new TFSimage() { _posX = _temposX, _posY = _temposY, _idata = _tdata });
                    }
                }
            }
        }

        public void Build_Image(int _paletteToUse)
        {
            //building image
            int baseX = 0;
            int baseY = 0;
            int topeX = 0;
            int topeY = 0;
            foreach (TFSimage subimage in _tfsImages)
            {
                topeX = Math.Max(topeX, (subimage._posX * 2) + 128);
                topeY = Math.Max(topeY, (subimage._posY) + 128);
            }

            //save true size for later
            _tfsimageWidthreal = topeX;
            _tfsimageHeightreal = topeY;

            _imagenfinal = new Bitmap(topeX, topeY);
            foreach (TFSimage subimage in _tfsImages)
            {
                baseX = subimage._posX * 2;
                baseY = subimage._posY;
                for (int i = 0; i < 128; i++)
                {
                    for (int j = 0; j < 128; j++)
                    {
                        _imagenfinal.SetPixel(baseX + i, baseY + j, _tfsPalette[_paletteToUse, subimage._idata[i, j]]);
                    }
                }

            }

            //draw image
            IntPtr ip = _imagenfinal.GetHbitmap();
            BitmapSource bs = null;
            try
            {
                bs = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(ip,
                   IntPtr.Zero, Int32Rect.Empty,
                   System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
            }
            finally
            {
            }

            _imagelodaded = true;
            Fotico.Source = bs;
        }


        private void Button_Load_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = ".tfs script files (*.tfs)|*.tfs";
            if (openFileDialog.ShowDialog() == true)
            {
                this.Title = appTitle + " - " + openFileDialog.FileName.Substring(openFileDialog.FileName.LastIndexOf("\\")+1);
                Load_Image(openFileDialog.FileName);
                Build_Image(0);
            }
        }

        private void Button_save_Click(object sender, RoutedEventArgs e)
        {
            if (_imagelodaded) { 

                SaveFileDialog saveFileDialog1 = new SaveFileDialog();

                saveFileDialog1.Filter = "png files (*.png)|*.png";

                if (saveFileDialog1.ShowDialog() == true)
                {
                    //check extension
                    string _extensorin = saveFileDialog1.FileName;
                    string _extensoron=" ";
                    int _testo = _extensorin.LastIndexOf('.');
                    if (_testo < 0)
                    {
                        _extensoron = " ";
                    }
                    else {
                        _extensoron = _extensorin.Substring(_testo, 4);
                    }

                    //save the file
                    if (_extensoron == ".png")
                    {
                        _imagenfinal.Save(_extensorin, ImageFormat.Png);
                       
                    }
                    else
                    {
                        _imagenfinal.Save(_extensorin + ".png", ImageFormat.Png);
                    }
                    MessageBox.Show( "PNG saved.", "Done", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                MessageBox.Show( "You need to load a TFS file first.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Button_import_Click(object sender, RoutedEventArgs e)
        {
            if (_imagelodaded) {
                int _errorcode = 0;
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Filter = ".png script files (*.png)|*.png";
                if (openFileDialog.ShowDialog() == true)
                {
                    _imagenimport = new Bitmap(openFileDialog.FileName);

                    //check size
                    if (_imagenimport.Width < _tfsimageWidthreal)
                    {
                        _errorcode = 1;
                    }
                    else if (_imagenimport.Width > _tfsimageWidthreal)
                    {
                        _errorcode = 3;
                    }
                    if (_imagenimport.Height < _tfsimageHeightreal)
                    {
                        _errorcode = 1;
                    }
                    else if (_imagenimport.Height > _tfsimageHeightreal)
                    {
                        _errorcode = 3;
                    }
                    if (_errorcode > 2)
                    {
                        MessageBoxResult dialogResult = MessageBox.Show( "Image importer is bigger than tfs files.\nSome image data will be lost.\nLoad anyways?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                        if (dialogResult == MessageBoxResult.Yes) {
                            _errorcode = 0;
                        }
                        if (dialogResult == MessageBoxResult.No) {
                            _errorcode = 3;
                        }
                    }

                    //start import

                    if (_errorcode < 1) { 
                        //building image
                        int baseX = 0;
                        int baseY = 0;
                        int topeX = 0;
                        int topeY = 0;
                        foreach (TFSimage subimage in _tfsImages)
                        {
                            topeX = Math.Max(topeX, (subimage._posX * 2) + 128);
                            topeY = Math.Max(topeY, (subimage._posY) + 128);
                        }

                        //check pixels
                        foreach (TFSimage subimage in _tfsImages)
                        {
                            baseX = subimage._posX * 2;
                            baseY = subimage._posY;

                            for (int i = 0; i < 128; i++)
                            {
                                for (int j = 0; j < 128; j++)
                                {
                                    Color _importpixelcolor = _imagenimport.GetPixel(baseX + i, baseY + j);
                                    if (_importpixelcolor != _tfsPalette[0, subimage._idata[i, j]])
                                    {
                                        bool _errorhere = true;
                                        //pixel is different
                                        for (int k = 0; k < 256; k++)
                                        {
                                            if (_importpixelcolor == _tfsPalette[0, k])
                                            {
                                                subimage._idata[i, j] = k;
                                                _errorhere = false;
                                                break;
                                            }
                                        }
                                        if (_errorhere)
                                        {
                                            _errorcode = 2;
                                        }

                                    }
                                }
                            }

                        }
                        if (_errorcode > 1) //palete error
                        {
                            MessageBox.Show( "Palette error, pixels without a color\nin the palette have been omitted", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        //else
                        {
                            //building image
                            baseX = 0;
                            baseY = 0;
                            topeX = 0;
                            topeY = 0;
                            foreach (TFSimage subimage in _tfsImages)
                            {
                                topeX = Math.Max(topeX, (subimage._posX * 2) + 128);
                                topeY = Math.Max(topeY, (subimage._posY) + 128);
                            }
                            _imagenfinal = new Bitmap(topeX, topeY);
                            foreach (TFSimage subimage in _tfsImages)
                            {
                                baseX = subimage._posX * 2;
                                baseY = subimage._posY;
                                for (int i = 0; i < 128; i++)
                                {
                                    for (int j = 0; j < 128; j++)
                                    {
                                        _imagenfinal.SetPixel(baseX + i, baseY + j, _tfsPalette[0, subimage._idata[i, j]]);
                                    }
                                }

                            }

                            //draw image
                            IntPtr ip = _imagenfinal.GetHbitmap();
                            BitmapSource bs = null;
                            try
                            {
                                bs = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(ip,
                                   IntPtr.Zero, Int32Rect.Empty,
                                   System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
                            }
                            finally
                            {
                            }

                            _imagelodaded = true;
                            Fotico.Source = bs;
                            MessageBox.Show( "PNG imported.", "Done", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                    else if (_errorcode == 1) //too small
                    {
                        MessageBox.Show( "Image importer is smaller than tfs files.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    else if (_errorcode == 3) //aborted
                    {
                        MessageBox.Show( "Image import aborted.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show( "You need to load a TFS file first.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Button_savetfs_Click(object sender, RoutedEventArgs e)
        {
            if (_imagelodaded)
            {
                SaveFileDialog saveFileDialog1 = new SaveFileDialog();
                byte[] _writer16;
                byte _writer8;
                saveFileDialog1.Filter = "tfs files (*.tfs)|*.tfs|All files (*.*)|*.*";
                saveFileDialog1.FilterIndex = 2;
                

                if (saveFileDialog1.ShowDialog() == true)
                {

                    //check extension
                    string _extensorin = saveFileDialog1.FileName;
                    string _extensoron = " ";
                    string _filetosave;
                    int _testo = _extensorin.LastIndexOf('.');
                    if (_testo < 0)
                    {
                        _extensoron = " ";
                    }
                    else
                    {
                        _extensoron = _extensorin.Substring(_testo, 4);
                    }


                    if (_extensoron == ".tfs")
                    {
                        _filetosave = _extensorin;
                    }
                    else
                    {
                        _filetosave = _extensorin + ".tfs";
                    }
                    //write the file
                    using (FileStream fs = new FileStream(_filetosave, FileMode.CreateNew, FileAccess.Write))
                {
                        using (BinaryWriter br = new BinaryWriter(fs, new ASCIIEncoding()))
                        {
                            //writing initial values
                            br.Seek(0, SeekOrigin.Begin);

                            _writer16 = BitConverter.GetBytes(Convert.ToUInt16(_tfsimageWidth));
                            br.Write(_writer16);
                            _writer16 = BitConverter.GetBytes(Convert.ToUInt16(_tfsimageHeight));
                            br.Write(_writer16);
                            _writer16 = BitConverter.GetBytes(Convert.ToUInt16(_tfsimagePaletteCount));
                            br.Write(_writer16);
                            _writer16 = BitConverter.GetBytes(Convert.ToUInt16(_tfsimagePadding));
                            br.Write(_writer16);

                            // writing Palettes

                            for (int i = 0; i < _tfsimagePaletteCount; i++)
                            {
                                for (int j = 0; j < 256; j++)
                                {
                                    _writer16 = BitConverter.GetBytes(Convert.ToUInt16(_tfsPalettevalue[i, j]));
                                    br.Write(_writer16);
                                }
                            }

                            //writing images

                            foreach (TFSimage subimage in _tfsImages)
                            {
                                _writer16 = BitConverter.GetBytes(Convert.ToUInt16(subimage._posX));
                                br.Write(_writer16);
                                _writer16 = BitConverter.GetBytes(Convert.ToUInt16(subimage._posY));
                                br.Write(_writer16);
                                for (int i = 0; i < 128; i++)
                                {
                                    for (int j = 0; j < 128; j++)
                                    {
                                        _writer8 = Convert.ToByte(subimage._idata[j,i]);
                                        br.Write(_writer8);
                                    }
                                }

                            }
                        }
                    }
                    MessageBox.Show("TFS saved.", "Done",  MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }

            else { 
                MessageBox.Show( "You need to load a TFS file first.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Combobox_palletesel_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_imagelodaded)
            {
                Build_Image(Combobox_palletesel.SelectedIndex);
            }
        }
    }
}
   
                            
            
                

           
