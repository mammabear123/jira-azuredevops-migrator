﻿using Common.Config;
using Migration.Common.Log;
using Migration.WIContract;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace JiraExport
{
    public static class LinkMapperUtils
    {

        public static void MapEpicChildLink(JiraRevision r, List<WiLink> links, string field, string type, ConfigJson config)
        {
            if (String.IsNullOrWhiteSpace(field))
            {
                throw new ArgumentNullException(nameof(field));
            }

            if (String.IsNullOrWhiteSpace(type))
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (r.Fields.TryGetValue(field, out object value))
            {
                var parentKeyStr = r.OriginId.Substring(r.OriginId.LastIndexOf("-", StringComparison.InvariantCultureIgnoreCase) + 1);
                var childKeyStr = value?.ToString().Substring((value.ToString()).LastIndexOf("-", StringComparison.InvariantCultureIgnoreCase) + 1);

                if (int.TryParse(parentKeyStr, out var parentKey) && int.TryParse(childKeyStr, out var childKey) && parentKey > childKey)
                    AddSingleLink(r, links, field, type, config);
            }
        }

        /// <summary>
        /// Add or remove single link
        /// </summary>
        /// <param name="r"></param>
        /// <param name="links"></param>
        /// <param name="field"></param>
        /// <param name="type"></param>
        /// <returns>True if link is added, false if it's not</returns>
        public static void AddRemoveSingleLink(JiraRevision r, List<WiLink> links, string field, string type, ConfigJson config)
        {
            if (String.IsNullOrWhiteSpace(field))
            {
                throw new ArgumentNullException(nameof(field));
            }

            if (String.IsNullOrWhiteSpace(type))
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (r.Fields.TryGetValue(field, out object value))
            {
                value = NumericCheckOnLinkTypeField(r, field, value);

                var changeType = value == null ? ReferenceChangeType.Removed : ReferenceChangeType.Added;
                var linkType = (from t in config.LinkMap.Links where t.Source == type select t.Target).FirstOrDefault();

                // regardless if action is add or remove, as there can be only one, we remove previous epic link if it exists
                if (r.Index != 0)
                {
                    var prevLinkValue = r.ParentItem.Revisions[r.Index - 1].GetFieldValue(field);
                    var prevLinkValueUnchecked = NumericCheckOnLinkTypeField(r, field, prevLinkValue);
                    if(prevLinkValueUnchecked != null)
                    {
                        prevLinkValue = prevLinkValueUnchecked.ToString();
                    }
                    // if previous value is not null, add removal of previous link
                    if (!string.IsNullOrWhiteSpace(prevLinkValue))
                    {
                        var removeLink = new WiLink()
                        {
                            Change = ReferenceChangeType.Removed,
                            SourceOriginId = r.ParentItem.Key,
                            TargetOriginId = prevLinkValue,
                            WiType = linkType
                        };

                        links.Add(removeLink);
                    }
                }

                if (changeType == ReferenceChangeType.Added)
                {
                    string linkedItemKey = (string)value;

                    var link = new WiLink()
                    {
                        Change = changeType,
                        SourceOriginId = r.ParentItem.Key,
                        TargetOriginId = linkedItemKey,
                        WiType = linkType
                    };

                    links.Add(link);
                }
            }
        }

        private static object NumericCheckOnLinkTypeField(JiraRevision r, string field, object value)
        {
            // 2023-12-05: For later versions pf Jira cloud, the parent link/epic link fields have been replaced by a single
            // field named "Parent". This is represented by the ParentItem field and r.field["parent"] instead holds the numeric ID.
            // Here we ensure that what we get is the issue key

            if (value != null)
            {
                bool isNumeric = int.TryParse(value.ToString(), out int n);
                if (isNumeric && field == "parent" && r.ParentItem != null && r.ParentItem.Parent != null)
                {
                    value = r.ParentItem.Parent;
                }
            }

            return value;
        }

        public static void AddSingleLink(JiraRevision r, List<WiLink> links, string field, string type, ConfigJson config)
        {
            if (String.IsNullOrWhiteSpace(field))
            {
                throw new ArgumentNullException(nameof(field));
            }

            if (String.IsNullOrWhiteSpace(type))
            {
                throw new ArgumentNullException(nameof(type));
            }


            if (r.Fields.TryGetValue(field, out object value))
            {
                var changeType = value == null ? ReferenceChangeType.Removed : ReferenceChangeType.Added;
                var linkType = (from t in config.LinkMap.Links where t.Source == type select t.Target).FirstOrDefault();


                if (changeType == ReferenceChangeType.Added)
                {
                    string linkedItemKey = (string)value;

                    if (string.IsNullOrEmpty(linkType))
                    {
                        Logger.Log(LogLevel.Warning, $"Cannot add 'Child' {linkedItemKey} link to 'Parent' {r.ParentItem.Key}, 'Child' link-map configuration missing.");
                        return;
                    }

                    var link = new WiLink()
                    {
                        Change = changeType,
                        SourceOriginId = r.ParentItem.Key,
                        TargetOriginId = linkedItemKey,
                        WiType = linkType,
                    };

                    links.Add(link);
                }
            }
        }

    }
}