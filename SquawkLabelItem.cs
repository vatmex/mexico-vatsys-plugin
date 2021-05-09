using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;
using System.Text.RegularExpressions;
using System.Collections.Concurrent;

using vatsys;
using vatsys.Plugin;

namespace MMFRVatsys.CustomLabels
{
    [Export(typeof(IPlugin))]
    public class SquawkLabelItem : IPlugin
    {
        /// The name of the custom label item we've added to Labels.xml in the Profile
        const string LABEL_ITEM = "LABEL_ITEM_DUPE";
        /// Dictionary to store the value we will retrieve when the label is painted. This avoids re-doing processing of the FDR every time the paint code is called. 
        ConcurrentDictionary<string, bool> ssrCodeValues = new ConcurrentDictionary<string, bool>();

        /// Plugin Name
        public string Name { get => "MMFR Squawk Validation"; }

        // Not needed for this plugin.
        public void OnFDRUpdate(FDP2.FDR updated) { }

        public void OnRadarTrackUpdate(RDP.RadarTrack updated)
        {
            // Prevents some errors while the coupling ocours.
            if (updated.CoupledFDR == null)
                return;

            // Validate if the callsign still exist. Otherwise remove from the Dict.
            if (FDP2.GetFDRIndex(updated.CoupledFDR.Callsign) == -1)
            {
                ssrCodeValues.TryRemove(updated.CoupledFDR.Callsign, out _);
            }
            else
            {
                bool codeValidity = this.isSSRCodeValid(updated.CoupledFDR, updated.ActualAircraft);
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

        private bool isSSRCodeValid(FDP2.FDR flightDataRecord, NetworkPilot actualAircraft)
        {
            // Allow Non-Discrete codes
            if (actualAircraft.TransponderCode == 0640) return true; // 1200
            if (actualAircraft.TransponderCode == 1024) return true; // 2000
            if (actualAircraft.TransponderCode == 3968) return true; // 7600
            if (actualAircraft.TransponderCode == 4032) return true; // 7700

            if (actualAircraft.TransponderCode != flightDataRecord.AssignedSSRCode) return false;

            return true;
        }
    }
}
