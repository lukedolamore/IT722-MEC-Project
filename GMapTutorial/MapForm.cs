﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using GMap.NET;
using GMap.NET.WindowsForms;
using GMap.NET.WindowsForms.Markers;
using GMap.NET.MapProviders;
using System.Net;
using System.Web.Script.Serialization;

namespace GMapTutorial
{
    public partial class MapForm : Form
    {
        List<PlaceOfInterest> placeOfInterests = new List<PlaceOfInterest>();
        List<int> UserIds = new List<int>();
        static List<PlaceOfInterest> SelectHull = new List<PlaceOfInterest>();

        public MapForm()
        {
            placeOfInterests.Clear();
            InitializeComponent();
            GetLocationData();
        }

        private void gmap_Load(object sender, EventArgs e)
        {          
            placeOfInterests.Sort();           
            gmap.DragButton = MouseButtons.Left;//Map can be moved using Left mouse button
            gmap.MapProvider = GoogleMapProvider.Instance;//Changed to Google maps
            GMaps.Instance.Mode = AccessMode.ServerOnly;
            //gmap.SetPositionByKeywords("Invercargill, New Zealand");
            gmap.Position = new PointLatLng(-46.4132, 168.3538);//Invercargill lat & lng
            gmap.ShowCenter = false;//get rids of red cross in the middle            
        }//gmap_Load

        private void gmap_OnMarkerClick(GMapMarker item, MouseEventArgs e)
        {
            Console.WriteLine(string.Format("Marker {0} was clicked.", item.Tag));
        }

        private void gmap_OnPolygonClick(GMapPolygon item, MouseEventArgs e)
        {
            Console.WriteLine(string.Format("Polygon {0} with tag {1} was clicked", item.Name, item.Tag));
        }
        
        private void GetLocationData()
        {
            string url = @"http://developer.kensnz.com/getlocdata";
            using (WebClient client = new WebClient())
            {
                var json = client.DownloadString(url);
                JavaScriptSerializer ser = new JavaScriptSerializer();
                var JSONArray = ser.Deserialize<Dictionary<string, string>[]>(json);
                //rTBnotes.AppendText(json + "\n\n");
                foreach (Dictionary<string, string> map in JSONArray)
                {
                    int userid = int.Parse(map["userid"]);
                    double latitude = double.Parse(map["latitude"]);
                    double longitude = double.Parse(map["longitude"]);
                    string description = map["description"];
                    PlaceOfInterest poi = new PlaceOfInterest(userid, latitude, longitude, description);
                    //listViewPOI.Items.Add(poi.UserID + " : " + poi.Latitude + " : " + poi.Longitude + " : " + poi.Description);
                    placeOfInterests.Add(poi);
                    if (!UserIds.Contains(poi.UserID))
                    {
                        UserIds.Add(poi.UserID);
                        CBofIds.Items.Add(poi.UserID);
                    }
                }
            }
        }//GetLocationData

        private void btnViewAll_Click(object sender, EventArgs e)
        {
            ViewAllForm viewAllForm = new ViewAllForm();
            viewAllForm.ShowDialog();
        }

        private void CBofIds_SelectedIndexChanged(object sender, EventArgs e)
        {
            int xid = int.Parse(CBofIds.SelectedItem.ToString());
            GMapOverlay markers = new GMapOverlay("markers");
            List<PointLatLng> rpoints = new List<PointLatLng>();
            foreach (var mark in placeOfInterests)
            {
                if (mark.UserID == xid)
                {
                    Console.WriteLine(mark.UserID);
                    GMapMarker marker = new GMarkerGoogle(new PointLatLng(mark.Latitude, mark.Longitude), GMarkerGoogleType.red_pushpin);
                    marker.ToolTipText = (string.Format("User ID :{0}\nDesc :{1}", mark.UserID, mark.Description));
                    marker.ToolTip.Stroke = Pens.Black;
                    marker.ToolTip.TextPadding = new Size(20, 20);
                    marker.ToolTip.Fill = new SolidBrush(Color.Green);
                    markers.Markers.Add(marker);
                    gmap.Overlays.Add(markers);
                }
                
            }
        }

        private void btnRemoveAll_Click(object sender, EventArgs e)
        {
           while (gmap.Overlays.Count > 0)
           {
                gmap.Overlays.RemoveAt(0);
           }
            SelectHull.Clear();
            gmap.Refresh();
        }

        private void btnDisplayAllMarkers_Click(object sender, EventArgs e)
        {
            int prevID = -1;
            Random random = new Random();
            Color prevColor = Color.Black;
            GMapOverlay markers = new GMapOverlay("markers");
            List<PointLatLng> rpoints = new List<PointLatLng>();
            foreach (var mark in placeOfInterests)
            {
                Console.WriteLine(mark.UserID);
                GMapMarker marker = new GMarkerGoogle(new PointLatLng(mark.Latitude, mark.Longitude), GMarkerGoogleType.red_pushpin);

                marker.ToolTipText = (string.Format("User ID :{0}\nDesc :{1}", mark.UserID, mark.Description));
                marker.ToolTip.Stroke = Pens.Black;
                marker.ToolTip.TextPadding = new Size(20, 20);
                if (mark.UserID != prevID)
                {
                    Color color = Color.FromArgb(random.Next(255), random.Next(255), random.Next(255));
                    prevID = mark.UserID;
                    prevColor = color;
                }
                if (mark.UserID == prevID)
                {
                    marker.ToolTip.Fill = new SolidBrush(prevColor);

                }
                markers.Markers.Add(marker);
                gmap.Overlays.Add(markers);
            }
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }
        //Convex Hull
        private void btnConvex_Click(object sender, EventArgs e)
        {
            if (CBofIds.SelectedItem == null)
            {
                MessageBox.Show("Please Remove all markers and Select a User ID");
            }
            else
            {
                int xid = int.Parse(CBofIds.SelectedItem.ToString());
                foreach (var mark in placeOfInterests)
                {
                    if (mark.UserID == xid)
                    {
                        SelectHull.Add(new PlaceOfInterest(mark.UserID, mark.Latitude, mark.Longitude, mark.Description));
                    }
                }
                List<PlaceOfInterest> ConvexHull = MakeHull(SelectHull);
                GMapOverlay routes = new GMapOverlay("routes");
                List<PointLatLng> points = new List<PointLatLng>();
                foreach (PlaceOfInterest poi in ConvexHull)
                {
                    points.Add(new PointLatLng(poi.Latitude, poi.Longitude));
                    GMapRoute route = new GMapRoute(points, "Convex Hull");
                    route.Stroke = new Pen(Color.Red, 3);
                    routes.Routes.Add(route);
                    gmap.Overlays.Add(routes);
                }
            }

        }

        private static List<PlaceOfInterest> MakeHull(List<PlaceOfInterest> selectHull)
        {
            List<PlaceOfInterest> newPoints = new List<PlaceOfInterest>(selectHull);
            newPoints.Sort();
            return HullSorted(newPoints);
        }

        private static List<PlaceOfInterest> HullSorted(List<PlaceOfInterest> newPoints)
        {
            List<PlaceOfInterest> upperHull = new List<PlaceOfInterest>();
            foreach (PlaceOfInterest p in SelectHull)
            {
                while (upperHull.Count >= 2)
                {
                    PlaceOfInterest q = upperHull[upperHull.Count - 1];
                    PlaceOfInterest r = upperHull[upperHull.Count - 2];
                    if ((q.Latitude - r.Latitude) * (p.Longitude - r.Longitude) >= (q.Longitude - r.Longitude) * (p.Latitude - r.Latitude))
                    {
                        upperHull.RemoveAt(upperHull.Count - 1);
                    }
                    else
                    {
                        break;
                    }
                }//While
                upperHull.Add(p);
            }
            upperHull.RemoveAt(upperHull.Count - 1);

            List<PlaceOfInterest> lowerHull = new List<PlaceOfInterest>();
            for (int i = SelectHull.Count - 1; i >= 0; i--)
            {
                PlaceOfInterest p = SelectHull[i];
                while (lowerHull.Count >= 2)
                {
                    PlaceOfInterest q = lowerHull[lowerHull.Count - 1];
                    PlaceOfInterest r = lowerHull[lowerHull.Count - 2];
                    if ((q.Latitude - r.Latitude) * (p.Longitude - r.Longitude) >= (q.Longitude - r.Longitude) * (p.Latitude - r.Latitude))
                    {
                        lowerHull.RemoveAt(lowerHull.Count - 1);
                    }
                    else
                    {
                        break;
                    }
                }
                lowerHull.Add(p);
            }
            lowerHull.RemoveAt(lowerHull.Count - 1);
            if (!(upperHull.Count == 1 && Enumerable.SequenceEqual(upperHull, lowerHull)))
            {
                upperHull.AddRange(lowerHull);
                upperHull.Add(SelectHull[0]);
            }
            return upperHull;
        }

        private void btnMEC_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Coming Soon");
        }
    }//Class
}//NS
