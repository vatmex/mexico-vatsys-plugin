using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;
using System.Text.RegularExpressions;
using vatsys.Plugin;
using System.Collections.Concurrent;

namespace vatsys.PBNCapPlugin
{
    [Export(typeof(IPlugin))]
    public class PBNCapLabelItem : IPlugin
    {
        /// The name of the custom label item we've added to Labels.xml in the Profile
        const string LABEL_ITEM = "PBN_CAP";
        /// Dictionary to store the value we will retrieve when the label is painted. This avoids re-doing processing of the FDR every time the paint code is called. 
        ConcurrentDictionary<string, char> pbnValues = new ConcurrentDictionary<string, char>();

        /// Plugin Name
        public string Name { get => "PBN Capability Label Item"; }

        /// When the FDR is updated we check if it still exists in the Flight Data Processor and remove from our dictionary if not. Otherwise we do some simple regex matching to find 
        /// the flight planned PBN category and store the character we want to display in the label in the dictionary.
        public void OnFDRUpdate(FDP2.FDR updated)
        {
            if (FDP2.GetFDRIndex(updated.Callsign) == -1)
                pbnValues.TryRemove(updated.Callsign, out _);
            else
            {
                Match pbn = Regex.Match(updated.Remarks, @"PBN\/\w+\s");
                bool rnp10 = Regex.IsMatch(pbn.Value, @"A\d");
                bool rnp5 = Regex.IsMatch(pbn.Value, @"B\d");
                bool rnp4 = Regex.IsMatch(pbn.Value, @"L\d");
                bool rnp2 = updated.Remarks.Contains("NAV/RNP2") || updated.Remarks.Contains("NAV/GLS RNP2");

                char cap = 'Z';
                if (rnp2 && (rnp10 || rnp4))
                    cap = 'A';
                else if (rnp2)
                    cap = '2';
                else if (rnp4)
                    cap = '4';
                else if (rnp5)
                    cap = '5';
                else if (rnp10)
                    cap = 'T';

                pbnValues.AddOrUpdate(updated.Callsign, cap, (k, v) => cap);
            }
        }

        /// Not needed for this plugin. But you could for instance, use the new position of the radar track or its change in state (cancelled, etc.) to do some processing. 
        public void OnRadarTrackUpdate(RDP.RadarTrack updated)
        {

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

            char val = 'Z';
            pbnValues.TryGetValue(flightDataRecord.Callsign, out val);

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
