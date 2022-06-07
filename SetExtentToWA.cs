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
using MessageBoxResult = System.Windows.MessageBoxResult;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxImage = System.Windows.MessageBoxImage;

namespace SetExtentToWA
{
    internal class SetExtentToWA : Button
    {
        protected override void OnClick()
        {
            var project = Project.Current;
            if (project == null)
            {
                MessageBox.Show("There are no currently active projects.", "No active projects", MessageBoxButton.OK, MessageBoxImage.Information);
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

            System.Windows.MessageBoxResult result = MessageBoxResult.Cancel;
            if (items.Any())
            {
                result = MessageBox.Show("Set all maps' extents to that of WA?", "Set all extents", MessageBoxButton.OKCancel, MessageBoxImage.Question);
            }

            if (result == MessageBoxResult.OK)
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
                        // If the map's projection is not the same as WA envelope, project the envelope to match map or vice-versa
                        // depending on user input.
                        Envelope projectedWaEnvelope = waEnvelope;
                        if (!waEnvelope.SpatialReference.IsEqual(map.SpatialReference))
                        {
                            // Ask the user if they want to change the map's projection to 2927.
                            // Yes: Map is projected to 2927
                            // No: WA envelope projected (resultin in a projected copy) to match map
                            result = MessageBox.Show($"Change map's projection to {waEnvelope.SpatialReference.Name}?", "Change map projection?", MessageBoxButton.YesNo, MessageBoxImage.Question);
                            if (result == MessageBoxResult.Yes)
                            {
                                map.SetSpatialReference(waEnvelope.SpatialReference);
                                currentFullExtent = map.CalculateFullExtent();
                            }
                            else
                            {
                                projectedWaEnvelope = (Envelope)geometryEngine.Project(waEnvelope, map.SpatialReference);
                            }
                        }


                        // Skip to next map if its extent is already WA
                        if (geometryEngine.Equals(currentFullExtent, projectedWaEnvelope))
                        {
                            nItem = new NotificationItem($"{DateTime.Now.Ticks}", false, $"Extent of map \"{map.Name}\" was not changed. Current full extent is already set to WA extent.", NotificationType.Information);
                            NotificationManager.AddNotification(nItem);
                            continue;
                        }

                        // Check to see if full extent of map contains WA extent.
                        bool fullExtentContainsWa = false;
                        try
                        {
                            fullExtentContainsWa = geometryEngine.Contains(currentFullExtent, projectedWaEnvelope);
                        }
                        catch (InvalidOperationException iox)
                        {
                            string messageText = $"The following error occured when checking to see if the map's full extent contained WA's extent:\n{iox.Message}";
                            MessageBox.Show(messageText,
                                            "Error when checking to see if full extent contains WA",
                                            MessageBoxButton.OK, MessageBoxImage.Error);
                            nItem = new NotificationItem($"{DateTime.Now.Ticks}", false, messageText, NotificationType.Error);
                            NotificationManager.AddNotification(nItem);
                            continue;
                        }

                        if (fullExtentContainsWa)
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
