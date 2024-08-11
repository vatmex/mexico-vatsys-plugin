using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;
using System.Text.RegularExpressions;
using System.Collections.Concurrent;
using vatsys;
using vatsys.Plugin;

// ReSharper disable All
#pragma warning disable IDE0060

namespace Vatmex.Vatsys.Plugins.RNPCaps
{
    [Export(typeof(IPlugin))]
    public class RnpCapLabelItem : IPlugin
    {
        // Plugin Name
        public string Name => "Vatmex RNP Capabilities Check";

        // Label name in Sector File
        // ReSharper disable once InconsistentNaming
        private const string LABEL_ITEM = "LABEL_ITEM_RNVCAP";

        // Dictionary to store the value we will retrieve when the label is painted. This avoids re-doing processing of the FDR every time the paint code is called. 
        private readonly ConcurrentDictionary<string, char> _pbnValues = new ConcurrentDictionary<string, char>();

        public void OnFDRUpdate(FDP2.FDR updated) 
        {
            if (FDP2.GetFDRIndex(updated.Callsign) == -1)
                _pbnValues.TryRemove(updated.Callsign, out _);
            else
            {
                Match pbn = Regex.Match(updated.Remarks, @"PBN\/\w+\s");
                bool rnp10 = Regex.IsMatch(pbn.Value, @"A\d");
                bool rnp5 = Regex.IsMatch(pbn.Value, @"B\d");
                bool rnp4 = Regex.IsMatch(pbn.Value, @"L\d");
                bool rnp2 = updated.Remarks.Contains("NAV/RNP2") || updated.Remarks.Contains("NAV/GLS RNP2");

                // TODO: Find a better way
                char cap = 'P';
                if (rnp2 && (rnp10 || rnp4))
                    cap = '\0';
                else if (rnp2)
                    cap = '\0';
                else if (rnp4)
                    cap = '\0';
                else if (rnp5)
                    cap = '\0';
                else if (rnp10)
                    cap = '\0';

                _pbnValues.AddOrUpdate(updated.Callsign, cap, (k, v) => cap);
            }
        }

        // Not needed for this plugin.
        public void OnRadarTrackUpdate(RDP.RadarTrack updated) { }

        /// First we check if the itemType is our custom Label Item, and a flight data record exists (since we need a callsign) 
        /// Then we get the previously calculated character from our dictionary and display it by returning a custom label item.
        /// Note we change the items color from the default color if it is a 'Z' char.
        public CustomLabelItem GetCustomLabelItem(string itemType, Track track, FDP2.FDR flightDataRecord, RDP.RadarTrack radarTrack)
        {
            if (flightDataRecord == null) return null;

            if (itemType != LABEL_ITEM) return null;

            char val = 'P';
            _pbnValues.TryGetValue(flightDataRecord.Callsign, out val);

            return new CustomLabelItem()
            {
                Type = itemType,
                ForeColourIdentity = Colours.Identities.Default,
                Text = val.ToString()
            };
        }

        //Here we can set a custom colour for the track and label. Otherwise return null.
        public CustomColour SelectASDTrackColour(Track track)
        {
            return null;
        }

        public CustomColour SelectGroundTrackColour(Track track)
        {
            return null;
        }
    }
}
