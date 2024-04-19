using DreamNode.Graph;
using DreamNode.Register;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
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

        private bool route = false;

        public MainWindow()
        {
            engine = new Engine();

            if (File.Exists(saveFile))
                engine.Restore(saveFile);

            engine.Start();

            InitializeComponent();

            multiInput.MakeComboBoxSearchable();
            startInput.MakeComboBoxSearchable();
            finishInput.MakeComboBoxSearchable();

            multiInput.ItemsSource = engine.pools.Select(p => p.id);
            startInput.ItemsSource = engine.pools.Select(p => p.id);
            finishInput.ItemsSource = engine.pools.Select(p => p.id);

            datagrid1.ItemsSource = engine.pools;


            Refresh();
        }

        /// <summary>
        /// Updates UI.
        /// </summary>
        public void Refresh()
        {
            this.PoolDescription.Text = active.desc;
            this.PoolId.Text = active.id;
            this.PoolSize.Content = active.size;


            status.Content = lookBack ? "lookback" : goToSubmenu.HasValue ? "Go To" : "";

            datagrid1.Items.Refresh();
            multiInput.ItemsSource = engine.pools.Select(p => p.id);

            GenerateOverview();

        }


        /// <summary>
        /// handles changing tabs
        /// </summary>
        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count != 1)
                return;
            if (e.AddedItems[0] is not TabItem)
                return;

            TabItem t = (e.AddedItems[0] as TabItem)!;

            if ((string)t.Header == "List")
            {
                resetDataGrid();

                datagrid1.Items.Refresh();
                datagrid2.Items.Refresh();

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


        /// <summary>
        /// Handles window closing
        /// </summary>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (engine.Save(saveFile))
                return;
            else
            {
                e.Cancel = true;
                return;
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////
        ///
        ///     Main view / explore
        ///
        ////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Creates center ascii text showing the neighboring rooms
        /// </summary>
        private void GenerateOverview()
        {
            const short wrapLength = 23;
            const short smallBoxHeight = 9;
            const short BigBoxHeight = 15;
            const short middleHeigth = 5;

            var source = active.GetPassages();

            Dictionary<PassageType, List<string>> end = new Dictionary<PassageType, List<string>>();

            foreach (KeyValuePair<PassageType, List<Passage>> item in source)
            {
                List<string> lines = new List<string>();

                foreach(Passage p in item.Value)
                {
                    short count = 0;

                    while (p.linkId.Length > count * (wrapLength - 3))
                    {

                        lines.Add(
                            (goToSubmenu.HasValue ? goToSubmenu.Value == item.Key ? $"{item.Value.IndexOf(p) + 1}: " : "   " : "   ")
                            + p.linkId.Substring(count++ * (wrapLength - 3), p.linkId.Length > count * (wrapLength - 3) ? wrapLength - 3 : p.linkId.Length % (wrapLength - 3))
                            + new string(' ', wrapLength - 3 - (p.linkId.Length % (wrapLength - 3))));
                    }

                }

                end.Add(item.Key, lines);

            }

            string wrapSpaces = new string(' ', wrapLength);
            string wrapSeparator = new string('=', wrapLength);


            StringBuilder sb = new();

            void sbAppend(PassageType pt, int line)
            {
                if(pt is PassageType.Up || pt is PassageType.South || pt is PassageType.Down)
                {
                    line -= 17;
                }

                if (end[pt].Count < line + 1)
                    sb.Append(wrapSpaces);
                else
                    sb.Append(end[pt][line]);
            }

            for (int line = 0; line < 25; line++)
            {

                if(line <= BigBoxHeight)
                    sbAppend(PassageType.West, line);
                else if (line == BigBoxHeight + 1)
                    sb.Append(wrapSeparator);
                else
                    sbAppend(PassageType.Up, line);

                sb.Append(' ');

                if (line <= smallBoxHeight)
                    sbAppend(PassageType.North, line);

                else if (line == smallBoxHeight + 1)
                    sb.Append(wrapSeparator);

                else if (line <= smallBoxHeight + 1 + middleHeigth)
                    sb.Append(wrapSpaces);

                else if (line == smallBoxHeight + 2 + middleHeigth)
                    sb.Append(wrapSeparator);

                else
                    sbAppend(PassageType.South, line);

                sb.Append(' ');


                if (line <= BigBoxHeight)
                    sbAppend(PassageType.East, line);
                else if (line == BigBoxHeight + 1)
                    sb.Append(wrapSeparator);
                else
                    sbAppend(PassageType.Down, line);

                sb.AppendLine();
            }

            this.PoolPassagesOverview.Content = sb.ToString();
        }


        /// <summary>
        /// handle numeric inputs, or clicks on the numerical buttons
        /// </summary>
        /// <param name="x">Key pressed</param>
        /// <param name="addPassage">false: go to room, true: create new room</param>
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

                    togo = passages[goToSubmenu.Value][index - 1];

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

        /// <summary>
        /// handles keyboard input
        /// </summary>
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if ((tabMenu.SelectedItem as TabItem).Header.ToString() != "Register")
                return;

            var x = e.Key;

            if (x == Key.Enter || x == Key.Escape)
                Keyboard.Focus(tabMenu);

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


        private void btnRoute_Click(object sender, RoutedEventArgs e)
        {
            if (startInput.Text == "" || finishInput.Text == "")
                return;


            Pool root = engine.pools.Find(p => p.id == startInput.Text);
            Pool goal = engine.pools.Find(p => p.id == finishInput.Text);

            if (root == null || goal == null)
                return;

            Queue<Pool> Q = new();
            HashSet<Pool> explored = new();

            Q.Enqueue(root);

            Pool v;

            while (Q.Count != 0)
            {
                v = Q.Dequeue();
                if (v == goal)
                    break;

                foreach(Pool w in v.passages.Select(p => p.link))
                {
                    if (w == null)
                        continue;

                    if (!explored.Contains(w))
                    {
                        explored.Add(w);
                        w.route = v;
                        Q.Enqueue(w);
                    }
                }   
            }

            if (Q.Count == 0)
                return;

            explored.Clear();
            v = goal;

            while(true)
            {
                Debug.WriteLine(v.id);
                explored.Add(v);
                if(v.route == root)
                {
                    root.route = v;
                    explored.Add(root);
                    break;
                }
                v = v.route;
            }

            foreach (Pool p in engine.pools.Except(explored))
                p.route = null;

            tabMenu.SelectedIndex = 2;

            route = true;
        }

        private void btnClearRoute_Click(object sender, RoutedEventArgs e)
        {
            if (route)
            {
                foreach (Pool p in engine.pools)
                    p.route = null;
                route = false;
            }
        }

        /// <summary>
        /// handles clicks on the virtual numeric keypad (the 9 buttons)
        /// </summary>
        private void NewPassageClick(object sender, RoutedEventArgs e)
        {
            NumInput((Key)(74 + ((sender as Button).Name.Last() - '0')));

        }

        /// <summary>
        /// handles changing the room name
        /// </summary>
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

        /// <summary>
        /// handle changing description of room
        /// </summary>
        private void PoolDescription_TextChanged(object sender, TextChangedEventArgs e)
        {
            active.desc = PoolDescription.Text;

            //TODO: handle same name
        }

        /// <summary>
        /// fusions two rooms into one
        /// </summary>
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

        /// <summary>
        /// adds a view between two rooms
        /// </summary>
        private void btnAddView_Click(object sender, RoutedEventArgs e)
        {
            if (multiInput.Text.Length == 0)
                return;

            Pool view = engine.pools.Where(p => p.id == multiInput.Text)?.First();

            if (view == null)
                return;

            engine.views.Add(new Tuple<Pool, Pool>(active, view));
        }


        /// <summary>
        /// handles save button 
        /// </summary>
        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            this.IsEnabled = false;
            bool x = engine.Save(saveFile);
            this.IsEnabled = true;
            Console.WriteLine(x);
        }

        ////////////////////////////////////////////////////////////////////////////////////////
        ///
        ///     List view
        ///
        ////////////////////////////////////////////////////////////////////////////////////////


        /// <summary>
        /// handles filtering on room list
        /// </summary>
        private void searchInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            var view = CollectionViewSource.GetDefaultView(datagrid1.ItemsSource);
            view.SortDescriptions.Add(new System.ComponentModel.SortDescription("id", System.ComponentModel.ListSortDirection.Ascending));

            view.Filter = (p) => (p as Pool).id.Contains((sender as TextBox).Text, StringComparison.CurrentCultureIgnoreCase);

        }

        /// <summary>
        /// resets sorting and filtering of the room list grids
        /// </summary>
        private void resetDataGrid()
        {
            var view = CollectionViewSource.GetDefaultView(datagrid1.ItemsSource);
            view.SortDescriptions.Clear();

            view.Filter = null;

            searchInput.Text = null;

            foreach (var column in datagrid1.Columns)
            {
                column.SortDirection = null;
            }
        }

        /// <summary>
        /// Updates right grid (in room list) according to selection in left grid
        /// </summary>
        private void datagrid1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count != 1)
                return;


            datagrid2.ItemsSource = (e.AddedItems[0] as Pool).passages;
            datagrid2.Items.Refresh();
        }


        /// <summary>
        /// teleports to the double clicked room in room list
        /// </summary>
        private void Row_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            var data = (sender as DataGridRow).DataContext;

            if (data is Pool)
                engine.GoTo(data as Pool, true);
            else if (data is Passage)
            {
                if ((data as Passage).link == null)
                    return;

                engine.GoTo((data as Passage).link, true);
            }

            Dispatcher.BeginInvoke((Action)(() => tabMenu.SelectedIndex = 1));
            // Some operations with this row
        }

        /// <summary>
        /// calls resetDataGrid() when button reset pressed
        /// </summary>
        private void btnReset_Click(object sender, RoutedEventArgs e)
        {
            resetDataGrid();
        }


        ////////////////////////////////////////////////////////////////////////////////////////
        ///
        ///     Graph
        ///
        ////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// load image in async. Calls GenerateView()
        /// </summary>
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


        /// <summary>
        /// Generates the graph view.
        /// </summary>
        private void GenerateView()
        {

            void writePool(StringBuilder sb, Pool p)
            {
                sb.Append("\"");
                sb.Append(p.id);
                sb.Append($"\"[width={Math.Pow(2, (int)p.size)}, height={Math.Pow(2, (int)p.size) * 0.7} {(p.route != null ? ", color = red" : "")}]");
                sb.AppendLine();
            }

            void writePassage(StringBuilder sb, Pool p, Passage a)
            {
                var q = p.id;
                var w = a.type.ToString().First();
                var e = a.linkId;
                bool isRoute = false;

                if (!a.link.passages.Any(x => x.link == p))
                    Console.Write("dfsuiy");

                var r = a.link.passages.Find(x => x.link == p).type.ToString().First();

                if (p.route == a.link || a.link.route == p)
                    isRoute = true;

                sb.AppendLine($"\"{q}\" -- \"{e}\" [taillabel = \"{w}\", headlabel = \"{r}\" {(isRoute ? ", color = red, labelfontcolor = red" : "")}]");
                //sb.AppendLine($"\"{q}\":{w} -- \"{e}\":{r}");
            }

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
                writePool(sb, p);
            }

            //Random rng = new Random();

            //var shuffledpools = engine.pools.OrderBy(_ => rng.Next()).ToList();

            HashSet<Tuple<Pool, Pool>> hashPassage = new();

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


                    if (hashPassage.Any(t => t.Item1 == p && t.Item2 == a.link || t.Item2 == p && t.Item1 == a.link))
                        continue;
                    else
                    {
                        writePassage(sb, p, a);
                        hashPassage.Add(new Tuple<Pool, Pool>(p, a.link));
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

        /// <summary>
        /// handles panning the graph view
        /// </summary>
        private void graphImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (graphImage.IsMouseCaptured) return;
            graphImage.CaptureMouse();

            start = e.GetPosition(border);
            origin.X = graphImage.RenderTransform.Value.OffsetX;
            origin.Y = graphImage.RenderTransform.Value.OffsetY;
        }

        /// <summary>
        /// handles panning the graph view
        /// </summary>
        private void graphImage_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            graphImage.ReleaseMouseCapture();
        }

        /// <summary>
        /// handles panning the graph view
        /// </summary>
        private void graphImage_MouseMove(object sender, MouseEventArgs e)
        {
            if (!graphImage.IsMouseCaptured) return;
            Point p = e.MouseDevice.GetPosition(border);

            Matrix m = graphImage.RenderTransform.Value;
            m.OffsetX = origin.X + (p.X - start.X);
            m.OffsetY = origin.Y + (p.Y - start.Y);

            graphImage.RenderTransform = new MatrixTransform(m);
        }

        /// <summary>
        /// handles zooming in graph view
        /// </summary>
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
    } 
}