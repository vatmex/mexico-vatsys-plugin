using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;
using System.Text.RegularExpressions;
using System.Collections.Concurrent;
using vatsys;
using vatsys.Plugin;
// ReSharper disable InconsistentNaming

namespace Vatmex.Vatsys.Plugins.CustomLabels
{
    [Export(typeof(IPlugin))]
    public class SquawkValidationLabelItem : IPlugin
    {
        // Plugin Name
        public string Name => "Vatmex Squawk Validation";

        // The name of the custom label item we've added to Labels.xml in the Profile
        // ReSharper disable once InconsistentNaming
        const string LABEL_ITEM = "LABEL_ITEM_DUPE";

        // Dictionary to store the value we will retrieve when the label is painted. This avoids re-doing processing of the FDR every time the paint code is called. 
        private readonly ConcurrentDictionary<string, bool> ssrCodeValues = new ConcurrentDictionary<string, bool>();

        // Not needed for this plugin.
        public void OnFDRUpdate(FDP2.FDR updated) { }

        public void OnRadarTrackUpdate(RDP.RadarTrack updated)
        {
            // Prevents some errors while the coupling occurs.
            if (updated.CoupledFDR == null)
                return;

            // Validate if the callsign still exist. Otherwise remove from the Dict.
            if (FDP2.GetFDRIndex(updated.CoupledFDR.Callsign) == -1)
            {
                ssrCodeValues.TryRemove(updated.CoupledFDR.Callsign, out _);
            }
            else
            {
                bool codeValidity = IsSsrCodeValid(updated.CoupledFDR, updated.ActualAircraft);
                ssrCodeValues.AddOrUpdate(updated.CoupledFDR.Callsign, codeValidity, (k, v) => codeValidity);
            }
        }

        /// First we check if the itemType is our custom Label Item, and a flight data record exists (since we need a callsign) 
        /// Then we get the previously calculated character from our dictionary and display it by returning a custom label item.
        /// Note we change the items colour from the default colour if it is a 'Z' char.
        public CustomLabelItem GetCustomLabelItem(string itemType, Track track, FDP2.FDR flightDataRecord, RDP.RadarTrack radarTrack)
        {
            if (flightDataRecord == null)
                return null;

            if (itemType != LABEL_ITEM)
                return null;

            // Check if the SSR is valid. Otherwise return the "Dupe" tag.
            ssrCodeValues.TryGetValue(flightDataRecord.Callsign, out bool isCodeValid);
            if (isCodeValid) return null;

            return new CustomLabelItem()
            {
                Type = itemType,
                ForeColourIdentity = Colours.Identities.Default,
                Text = "Dupe"
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

        private static bool IsSsrCodeValid(FDP2.FDR flightDataRecord, NetworkPilot actualAircraft)
        {
            // Allow Non-Discrete codes
            switch (actualAircraft.TransponderCode)
            {
                case 0640: // 1200
                    return true;
                case 0832: // 1500
                    return true;
                case 1024: // 2000
                    return true;
                case 3968: // 7600
                    return true;
                case 4032: // 7700
                    return true; 
            }

            return actualAircraft.TransponderCode == flightDataRecord.AssignedSSRCode;
        }
    }
}
