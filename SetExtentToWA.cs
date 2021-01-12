using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Catalog;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Editing;
using ArcGIS.Desktop.Extensions;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Dialogs;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Layouts;
using ArcGIS.Desktop.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SetExtentToWA
{
    internal class SetExtentToWA : Button
    {
        protected override void OnClick()
        {
            var project = Project.Current;
            if (project == null)
            {
                MessageBox.Show("There are no currently active projects.", "No active projects", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                return;
            }

            double xmin = 541593.3869217434,
            ymin = 75366.04199113384,
            xmax = 2560080.6803334514,
            ymax = 1383832.028539666;

            var sr = SpatialReferenceBuilder.CreateSpatialReference(2927);
            var envelope = EnvelopeBuilder.CreateEnvelope(xmin, ymin, xmax, ymax, sr);



            var items = project.GetItems<MapProjectItem>();

            System.Windows.MessageBoxResult result = System.Windows.MessageBoxResult.Cancel;
            if (items.Any())
            {
                result = MessageBox.Show("Set all maps' extents to that of WA?", "Set all extents", System.Windows.MessageBoxButton.OKCancel, System.Windows.MessageBoxImage.Question);
            }

            if (result == System.Windows.MessageBoxResult.OK)
            {
                QueuedTask.Run(() =>
                {
                    foreach (var item in items)
                    {
                        var map = item.GetMap();
                        map.SetCustomFullExtent(envelope);
                    }
                });
            }
        }
    }
}
