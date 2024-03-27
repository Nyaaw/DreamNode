using DreamNode.Graph;
using DreamNode.Register;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static System.Net.Mime.MediaTypeNames;


namespace DreamNode
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string imageFile = "G:\\Documents\\Code\\DreamNode\\Graph.png";
#if DEBUG
        private const string saveFile = "G:\\Documents\\Code\\DreamNode\\DebugGraph.txt";
#else
        private const string saveFile = "G:\\Documents\\Code\\DreamNode\\Graph.txt";
#endif
        private Engine engine;
        private Pool active => engine.active;

        private Point origin;  // Original Offset of image
        private Point start;   // Original Position of the mouse

        private PassageType? goToSubmenu = null;
        private int submenuPrestige = 0;
        private bool lookBack = false;
        public MainWindow()
        {
            engine = new Engine();

            if (File.Exists(saveFile))
                engine.Restore(saveFile);

            engine.Start();

            InitializeComponent();

            datagrid1.ItemsSource = engine.pools;
            multiInput.ItemsSource = engine.pools.Select(p => p.id);

            Refresh();
        }

        public void Refresh()
        {
            this.PoolDescription.Text = active.desc;
            this.PoolId.Text = active.id;
            this.PoolSize.Content = active.size;


            status.Content = lookBack ? "lookback" : goToSubmenu.HasValue ? "Go To" : "";

            datagrid1.Items.Refresh();


            if (goToSubmenu.HasValue)
            {
                var passages = active.GetPassages()[goToSubmenu.Value];

                StringBuilder sb = new();
                for (int i = 1; i < 9; i++)
                {
                    int index = submenuPrestige * 9 + i - 1;
                    if (passages.Length == index)
                    {
                        sb.AppendLine();
                        break;
                    }

                    sb.AppendLine($"{i}: {passages[index].link?.id ?? "none"}");
                }

                if (passages.Length > 9)
                {
                    sb.AppendLine("0: Page suivante");
                    sb.AppendLine($"Page {submenuPrestige}/{passages.Length / 9}");
                }


                this.PoolPassagesOverview.Content = sb.ToString();
            }

            else
            {
                var passagesCount = active.PassageCount();
                PassageType? oldDir = active.passages.Where(p => p.link != null && p.link == engine.old)?.FirstOrDefault()?.type;

                string Decorate(PassageType type)
                {
                    if (oldDir == type)
                        return $"[{passagesCount[type]}]";
                    else
                        return passagesCount[type].ToString();
                }

                this.PoolPassagesOverview.Content =
$@"        {Decorate(PassageType.North)}

{Decorate(PassageType.West)}                {Decorate(PassageType.East)}

        {Decorate(PassageType.South)}

Up: {Decorate(PassageType.Up)}
Down: {Decorate(PassageType.Down)}

Plus: {Decorate(PassageType.Plus)}";
            }



        }

        private void GenerateView()
        {
            StringBuilder sb = new();

            sb.Append(@" // DreamNode

strict graph ip_map {

// settings
node [
fontsize = ""16""
shape = ""box""
];
");


            sb.AppendLine();
            sb.AppendLine("//pools");
            foreach (var p in engine.pools)
            {
                sb.Append("\"");
                sb.Append(p.id);
                sb.Append($"\"[width={Math.Pow(2, (int)p.size)}, height={Math.Pow(2, (int)p.size) * 0.7}]");
                sb.AppendLine();
            }

            //Random rng = new Random();

            //var shuffledpools = engine.pools.OrderBy(_ => rng.Next()).ToList();

            HashSet<Tuple<Pool, Pool>> hash = new();

            sb.AppendLine();
            sb.AppendLine("//passages");

            int nullcount = 0;

            foreach (var p in engine.pools)
            {
                foreach (var a in p.passages)
                {
                    if (a.link == null)
                    {
                        sb.AppendLine($"null{++nullcount} [shape = circle, label=\"\", width=0.12]");
                        sb.AppendLine($"\"{p.id}\" -- null{nullcount} [taillabel=\"{a.type.ToString().First()}\"]");
                        continue;
                    }
                        

                    if (hash.Any(t => t.Item1 == p && t.Item2 == a.link || t.Item2 == p && t.Item1 == a.link))
                        continue;
                    else
                    {
                        var q = p.id;
                        var w = a.type.ToString().First();
                        var e = a.linkId;
                        var r = a.link.passages.Find(x => x.link == p).type.ToString().First();

                        sb.AppendLine($"\"{q}\" -- \"{e}\" [taillabel = \"{w}\", headlabel = \"{r}\"]");
                        //sb.AppendLine($"\"{q}\":{w} -- \"{e}\":{r}");
                        hash.Add(new Tuple<Pool, Pool>(p, a.link));
                    }
                }
            }

            sb.AppendLine("//views");

            foreach (var v in engine.views)
            {
                sb.AppendLine($"\"{v.Item1.id}\" -- \"{v.Item2.id}\" [style=dotted]");
            }

            sb.AppendLine("}");

            File.WriteAllText("G:\\Documents\\Code\\DreamNode\\GraphVisu.txt", sb.ToString());

            if (File.Exists(imageFile))
                File.Delete(imageFile);

            //engine.Save(saveFile);

            Process process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.WindowStyle = ProcessWindowStyle.Normal;
            startInfo.FileName = "C:\\Program Files\\Graphviz\\bin\\dot.exe";
            startInfo.WorkingDirectory = "G:\\Documents\\Code\\DreamNode";
            startInfo.Arguments = $"-Tpng -o {imageFile} G:\\Documents\\Code\\DreamNode\\GraphVisu.txt";
            startInfo.CreateNoWindow = true;



            process.StartInfo = startInfo;
            process.Start();
            process.WaitForExit();

        }

        private void resetDataGrid()
        {
            var view = CollectionViewSource.GetDefaultView(datagrid1.ItemsSource);
            view?.SortDescriptions.Clear();

            foreach (var column in datagrid1.Columns)
            {
                column.SortDirection = null;
            }
        }

        private void NumInput(Key x, bool addPassage = false)
        {


            PassageType pt;

            switch (x)
            {
                case Key.NumPad1: pt = PassageType.Plus; break;
                case Key.NumPad2: pt = PassageType.South; break;
                case Key.NumPad3: pt = PassageType.Down; break;
                case Key.NumPad4: pt = PassageType.West; break;
                case Key.NumPad5: pt = PassageType.Plus; break;
                case Key.NumPad6: pt = PassageType.East; break;
                case Key.NumPad8: pt = PassageType.North; break;
                case Key.NumPad9: pt = PassageType.Up; break;
                default: return;
            }


            if (lookBack) {

                if (x == Key.NumPad5)
                    active.passages.First().type = Passage.reverse(engine.old.passages.Where(p => p.link == active).First().type);
                else
                    active.passages.First().type = pt;

                lookBack = false;
            }
            else if (addPassage)
            {

                engine.AddEmptyPassage(pt);
            }
            else
            {
                Passage togo;
                var passages = active.GetPassages();


                if (goToSubmenu.HasValue)
                {
                    int index = x - Key.NumPad0;

                    if (index == 0)
                    {

                        submenuPrestige++;
                        return;
                    }

                    togo = passages[goToSubmenu.Value][9 * submenuPrestige + index - 1];

                    goToSubmenu = null;
                }
                else if (passages[pt].Count() == 0)
                    return;
                else if (passages[pt].Count() == 1)
                    togo = passages[pt][0];
                else {
                    goToSubmenu = pt;
                    Refresh();
                    return;
                }


                if (togo.link == null)
                {
                    lookBack = true;
                    togo.link = engine.NewPool();
                    togo.link.passages.Add(new Passage { link = active, type = PassageType.Plus });
                }

                engine.GoTo(togo.link);
            }

            Refresh();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if ((tabMenu.SelectedItem as TabItem).Header.ToString() != "Register")
                return;

            var x = e.Key;

            if (x == Key.Enter || x == Key.Escape)
                Keyboard.Focus(gridRegister);

            if (PoolId.IsFocused || PoolDescription.IsFocused || multiInput.IsFocused)
            {
                return;
            }
            
            
            if (x >= Key.NumPad0 && x <= Key.NumPad9)
                NumInput(e.Key, e.KeyboardDevice.IsKeyDown(Key.LeftCtrl));
            else if (x == Key.Add)
                active.size = Graph.PoolSize.Big;
            else if (x == Key.Subtract)
                active.size = Graph.PoolSize.Small;
            else if (x == Key.Multiply)
                active.size = Graph.PoolSize.Huge;

            else if (x == Key.F2)
            {
                PoolId.Focus();
                PoolId.SelectAll();

            }
            else if (x == Key.F3)
            {
                PoolDescription.Focus();
                PoolDescription.SelectAll();
            }
            else if (x == Key.F4)
            {
                multiInput.Focus();
            }

            Refresh();
        }

        private void PoolDescription_TextChanged(object sender, TextChangedEventArgs e)
        {
            active.desc = PoolDescription.Text;
        }

        private void NewPassageClick(object sender, RoutedEventArgs e)
        {
            NumInput((Key)(74 + ((sender as Button).Name.Last() - '0')));

        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            engine.Save(saveFile);
        }

        private void PoolId_TextChanged(object sender, TextChangedEventArgs e)
        {
            active.id = PoolId.Text;
        }

        private void btnTeleport_Click(object sender, RoutedEventArgs e)
        {
            if (multiInput.Text.Length == 0)
                return;

            Pool? tpto = engine.pools.Where(p => p.id == multiInput.Text).First();

            if (tpto == null)
                return;

            engine.GoTo(tpto, true);
            Refresh();
        }

        private void btnFusion_Click(object sender, RoutedEventArgs e)
        {
            if (multiInput.Text.Length == 0)
                return;

            Pool? fusion = engine.pools.Where(p => p.id == multiInput.Text).First();
            Pool stays;

            int dif = active.passages.Count - fusion.passages.Count;

            if (dif > 0)
                stays = active;
            else
            {
                stays = fusion;
                fusion = active;

                engine.GoTo(stays, true);

                Refresh();
            }

            stays.passages.Add(fusion.passages.First());

            fusion.passages.First().link.passages.Find(p => p.link == fusion).link = stays;

            engine.pools.Remove(fusion);

            Refresh();
        }

        private void btnAddView_Click(object sender, RoutedEventArgs e)
        {
            if (multiInput.Text.Length == 0)
                return;

            Pool view = engine.pools.Where(p => p.id == multiInput.Text)?.First();

            if (view == null)
                return;

            engine.views.Add(new Tuple<Pool, Pool>(active, view));
        }

        private void datagrid1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count != 1)
                return;


            datagrid2.ItemsSource = (e.AddedItems[0] as Pool).passages;
            datagrid2.Items.Refresh();
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count != 1)
                return;
            if (e.AddedItems[0] is not TabItem)
                return;

            TabItem t = (e.AddedItems[0] as TabItem)!;

            if ((string)t.Header == "List")
            {
                datagrid1.Items.Refresh();
                datagrid2.Items.Refresh();

                resetDataGrid();
                

                datagrid1.SelectedItem = active;
            }
            else if ((string)t.Header == "Register")
            {
                datagrid2.ItemsSource = null;
                datagrid1.SelectedItem = null;
                Refresh();
            }
            else if ((string)t.Header == "View")
            {
                Task.Run(LoadImage);
            }
        }

        private async Task LoadImage()
        {
            ReloadStatus.Dispatcher.Invoke(() =>
            {
                ReloadStatus.Visibility = Visibility.Visible;
            });

            GenerateView();

            graphImage.Dispatcher.Invoke(() =>  {
                var image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                image.UriSource = new Uri(imageFile);
                image.EndInit(); 
                graphImage.Source = image;
                ReloadStatus.Visibility = Visibility.Hidden;
                Refresh();
            });
        }

        private void graphImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (graphImage.IsMouseCaptured) return;
            graphImage.CaptureMouse();

            start = e.GetPosition(border);
            origin.X = graphImage.RenderTransform.Value.OffsetX;
            origin.Y = graphImage.RenderTransform.Value.OffsetY;
        }

        private void graphImage_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            graphImage.ReleaseMouseCapture();
        }

        private void graphImage_MouseMove(object sender, MouseEventArgs e)
        {
            if (!graphImage.IsMouseCaptured) return;
            Point p = e.MouseDevice.GetPosition(border);

            Matrix m = graphImage.RenderTransform.Value;
            m.OffsetX = origin.X + (p.X - start.X);
            m.OffsetY = origin.Y + (p.Y - start.Y);

            graphImage.RenderTransform = new MatrixTransform(m);
        }

        private void graphImage_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            Point p = e.MouseDevice.GetPosition(graphImage);

            Matrix m = graphImage.RenderTransform.Value;
            if (e.Delta > 0)
                m.ScaleAtPrepend(1.1, 1.1, p.X, p.Y);
            else
                m.ScaleAtPrepend(1 / 1.1, 1 / 1.1, p.X, p.Y);

            graphImage.RenderTransform = new MatrixTransform(m);
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            this.IsEnabled = false;
            engine.Save(saveFile);
            this.IsEnabled = true;

        }

        private void searchInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            
        }

        private void btnReset_Click(object sender, RoutedEventArgs e)
        {
            resetDataGrid();
        }

    } 
}