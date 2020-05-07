using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TorrentMiner
{
    public class Resources
    {
        bool isSelected = false;
        long index = 0;
        string name;
        string identify;
        bool isContainView = false;
        string time;
        double size = 0;
        string preview;

        public bool IsSelected
        {
            get
            {
                return isSelected;
            }

            set
            {
                isSelected = value;
            }
        }

        public long Index
        {
            get
            {
                return index;
            }

            set
            {
                index = value;
            }
        }

        public string Name
        {
            get
            {
                return name;
            }

            set
            {
                name = value;
            }
        }

        public string Identify
        {
            get
            {
                return identify;
            }

            set
            {
                identify = value;
            }
        }

        public bool IsContainView
        {
            get
            {
                return isContainView;
            }

            set
            {
                isContainView = value;
            }
        }

        public string Time
        {
            get
            {
                return time;
            }

            set
            {
                time = value;
            }
        }

        public double Size
        {
            get
            {
                return size;
            }

            set
            {
                size = value;
            }
        }

        public string Preview
        {
            get
            {
                return preview;
            }

            set
            {
                preview = value;
            }
        }
    }
}
