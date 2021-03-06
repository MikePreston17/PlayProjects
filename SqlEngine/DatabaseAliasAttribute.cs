﻿using System;

namespace DataAccess
{
    [AttributeUsage(AttributeTargets.Property)]
    public class DatabaseAliasAttribute : Attribute
    {
        public string Alias { get; set; }
        public DatabaseAliasAttribute(string alias)
        {
            Alias = alias;
        }

    }
}
