using UnityEngine;

namespace BCIEssentials.Utilities
{
    public class Custom_ColorContrast : MonoBehaviour 
    {
        private Color flashOnColor;
        private int contrastLevel;

        public void SetContrast(int _contrastLevel)
        {
            contrastLevel = _contrastLevel;
        }

        public int GetContrast()
        {
            return contrastLevel;
        }

        public Color Grey()
        {
            //R/G/B = 218, Luminance = 205, 15.02:1, #DADADA
            Color _maximum = new Color(0.8549f, 0.8549f, 0.8549f, 1f);

            //R/G/B = 156, Luminance = 147, 7.64:1, #9C9CBC
            Color step1 = new Color(0.601176f, 0.6011176f, 0.601176f, 1f);

            //R/G/B = 107, Luminance = 101, #6B6B6B
            Color step2 = new Color(0.4196f, 0.4196f, 0.4196f, 1f);

            //R/G/B = 63, L = 59, 1.99:1, #3F3F3F
            Color _minimum = new Color(0.2471f, 0.2471f, 0.2471f, 1f);

            Color _off = new Color(0,0,0,0);

            if(GetContrast() == 100)
            {
                flashOnColor = _maximum;
            }

            if(GetContrast() == 50) 
            {
                flashOnColor = step1;
            }

            if(GetContrast() == 25)
            {
                flashOnColor = step2;
            }
            
            if(GetContrast() == 10)
            {
                flashOnColor = _minimum;
            }

            if(GetContrast() == 0)
            {
                flashOnColor = _off;
            }

            return flashOnColor;
        }
    }
 }
