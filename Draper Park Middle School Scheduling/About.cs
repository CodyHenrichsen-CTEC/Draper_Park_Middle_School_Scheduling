﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Draper_Park_Scheduler
{
    public partial class About : Form
    {
        public About()
        {
            InitializeComponent();
            versionLabel.Text = "Current version: " + Application.ProductVersion;
        }
    }
}
