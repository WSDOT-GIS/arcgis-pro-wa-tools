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
            var waEnvelope = EnvelopeBuilder.CreateEnvelope(xmin, ymin, xmax, ymax, sr);
            // Create polygon for clipping
            var waPolygon = PolygonBuilder.CreatePolygon(waEnvelope);
            var lineSymbolForCrop = new CIMLineSymbol();


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
                    var geometryEngine = GeometryEngine.Instance;
                    foreach (var item in items)
                    {
                        var map = item.GetMap();
                        // Get the current full extent of the map.
                        var currentFullExtent = map.CalculateFullExtent();
                        // Set the map's full extent to WA if WA is contained in the current full extent.
                        NotificationItem nItem;
                        bool extentWasChanged = false;
                        // If the map's projection is not the same as WA envelope, project the envelope to match map.
                        var projectedWaEnvelope = !waEnvelope.SpatialReference.IsEqual(map.SpatialReference) ? (Envelope)geometryEngine.Project(waEnvelope, map.SpatialReference) : waEnvelope;
                        if (geometryEngine.Contains(currentFullExtent, projectedWaEnvelope))
                        {
                            map.SetCustomFullExtent(projectedWaEnvelope);
                            extentWasChanged = true;
                            nItem = new NotificationItem($"{DateTime.Now.Ticks}", false, $"Extent of map \"{map.Name}\" set to WA.", NotificationType.Information);
                        }
                        else
                        {
                            nItem = new NotificationItem($"{DateTime.Now.Ticks}", false, $"Extent of map \"{map.Name}\" was not changed. Current full extent does not contain WA.", NotificationType.Information);
                        }
                        NotificationManager.AddNotification(nItem);

                        if (extentWasChanged)
                        {
                            var currentClipGeometry = map.GetClipGeometry();
                            if (currentClipGeometry == null)
                            {
                                map.SetClipGeometry(waPolygon, lineSymbolForCrop);
                            }
                        }
                    }
                });
            }
        }
    }
}
