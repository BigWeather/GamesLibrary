using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
#if Allow_XNA
using Microsoft.Xna.Framework;
#endif

namespace GamesLibrary
{
    public class WorldModel
    {
        public class WorldLocation
        {
            public float longitude { get; private set; }
            public float latitude { get; private set; }
            public Hemisphere latitudeHemisphere
            {
                get
                {
                    if (latitude >= 0)
                        return Hemisphere.North;
                    return Hemisphere.South;
                }
            }
            public Hemisphere longitudeHemisphere
            {
                get
                {
                    if (longitude >= 0)
                        return Hemisphere.East;
                    return Hemisphere.West;
                }
            }

            public WorldLocation(float longitude, float latitude)
            {
                this.longitude = longitude;
                this.latitude = latitude;
            }
        }

        public enum Season { Spring, Summer, Fall, Winter }
        public enum Hemisphere { North, East, West, South }
        public enum Zone { Polar, Temperate, Tropical }

        #region public members
        public int widthTiles { get; private set; }
        public int heightTiles { get; private set; }
        #endregion

        #region private members
        private float _longitudePerTile;
        private float _latitudePerTile;

        private float _longitudeLeft;
        private float _latitudeTop;
        private float _longitudeWidth;
        private float _latitudeHeight;

        private float _axisTilt;
        private float _latitudeTropicOfCancer;
        private float _latitudeTropicOfCapricorn;
        private float _latitudeArcticCircle;
        private float _latitudeAntarcticCircle;

        private float _daysInYear;
        #endregion

        #region constructors
        public WorldModel(int widthTiles, int heightTiles) : this(widthTiles, heightTiles, new Rectangle(-180, 90, 360, 180), 23.5f, 365.25f) {}
        public WorldModel(int widthTiles, int heightTiles, Rectangle extentsLongitudeLatitude, float axisTilt, float daysInYear)
        {
            this.widthTiles = widthTiles;
            this.heightTiles = heightTiles;

            _longitudeLeft = (float)extentsLongitudeLatitude.X;
            _latitudeTop = (float)extentsLongitudeLatitude.Y;
            _longitudeWidth = (float)extentsLongitudeLatitude.Width;
            _latitudeHeight = (float)extentsLongitudeLatitude.Height;

            _longitudePerTile = _longitudeWidth / (float)widthTiles;
            _latitudePerTile = _latitudeHeight / (float)heightTiles;

            _axisTilt = axisTilt;
            _latitudeTropicOfCancer = axisTilt;
            _latitudeTropicOfCapricorn = -axisTilt;
            _latitudeArcticCircle = 90 - axisTilt;
            _latitudeAntarcticCircle = -90 + axisTilt;

            _daysInYear = daysInYear;
        }
        #endregion

        #region public methods
        public WorldLocation getWorldLocation(int x, int y)
        {
            float longitude = _longitudeLeft + ((float)x * _longitudePerTile);
            float latitude = _latitudeTop - ((float)y * _latitudePerTile);

            return new WorldLocation(longitude, latitude);
        }

        public Season getEffectiveSeason(int x, int y, int dayOfYear)
        {
            WorldLocation worldLocation = getWorldLocation(x, y);
            Zone zone = getZone(x, y);

            // Summer within the tropical zone.
            if (zone == Zone.Tropical)
                return Season.Summer;

            // Winter within the polar zone.
            if (zone == Zone.Polar)
                return Season.Winter;

            // NOTE: The following assumes a split of seasons of roughly each being
            //       a quarter of the days of the year.  This could very well be true
            //       only because earth has a tilt angle of 23.5 (roughly one-quarter of
            //       90 degrees), so this code may need to be modified to account somehow
            //       for differing season lengths due to tilt angle.

            // Consider the dead of summer / winter to be the middle of the year.
            // TODO: Account for seasonal lag at some point in the future?

            float midpointOfYear = _daysInYear / 2.0f;
            float seasonLength = _daysInYear / 4.0f;

#if false
            float summerHeight = midpointOfYear;
            float distanceFromSummerHeight = Math.Abs(summerHeight - (float)dayOfYear);

            // Test to see if it is clearly summer or winter...
            if (distanceFromSummerHeight <= (seasonLength / 2.0f))
                return (worldLocation.latitudeHemisphere == Hemisphere.North) ? Season.Summer : Season.Winter;
            else if (distanceFromSummerHeight > ((seasonLength / 2.0f) + seasonLength))
                return (worldLocation.latitudeHemisphere == Hemisphere.North) ? Season.Winter : Season.Summer;

            float distanceFromEquator = Math.Abs(worldLocation.latitude);
            float temperateZoneHeight = _latitudeArcticCircle - _latitudeTropicOfCancer;

            float transitionZoneHeight = temperateZoneHeight / 2.0f;

            // Figure the offset based on the day.  The start day for the transition season is the roughly the first day
            // of spring or fall.
            int startDay = (int)seasonLength;
            if (dayOfYear > midpointOfYear)
                startDay = (int)seasonLength * 3;
#endif

            bool northernHemisphere = (worldLocation.latitudeHemisphere == Hemisphere.North);

            float adjustedDay = dayOfYear;
            if (adjustedDay > midpointOfYear)
                adjustedDay -= midpointOfYear;

            if (adjustedDay < seasonLength)
            {
                if (dayOfYear > midpointOfYear)
                    return (northernHemisphere ? Season.Summer : Season.Winter);
                else
                    return (northernHemisphere ? Season.Winter : Season.Summer);
            }

            float percentIntoSeason = (adjustedDay - seasonLength) / seasonLength;

            float tropicsEndPoint = _axisTilt / 2.0f;
            float polarEndPoint = 90.0f - (_axisTilt / 2.0f);
            float distanceLatitude = polarEndPoint - tropicsEndPoint;
            float transitionZoneHeight = _axisTilt;

            if (dayOfYear > midpointOfYear)
            {
                // Fall in north, spring in south.
                float transitionZoneCenter = polarEndPoint - (percentIntoSeason * distanceLatitude);
                if (!northernHemisphere)
                    transitionZoneCenter -= 90.0f;

                if (Math.Abs(transitionZoneCenter - worldLocation.latitude) < (transitionZoneHeight / 2.0f))
                    return (northernHemisphere ? Season.Fall : Season.Spring);

                if (Math.Abs(worldLocation.latitude) < Math.Abs(transitionZoneCenter))
                    return Season.Summer;

                return Season.Winter;
            }
            else
            {
                // Spring in north, fall in south.
                float transitionZoneCenter = tropicsEndPoint + (percentIntoSeason * distanceLatitude);
                if (!northernHemisphere)
                    transitionZoneCenter -= 90.0f;

                if (Math.Abs(transitionZoneCenter - worldLocation.latitude) < (transitionZoneHeight / 2.0f))
                    return (northernHemisphere ? Season.Spring : Season.Fall);

                if (Math.Abs(worldLocation.latitude) < Math.Abs(transitionZoneCenter))
                    return Season.Summer;

                return Season.Winter;
            }

#if false
            if (dayOfYear > midpointOfYear)
                return (worldLocation.latitudeHemisphere == Hemisphere.North) ? Season.Fall : Season.Spring;
            else
                return (worldLocation.latitudeHemisphere == Hemisphere.North) ? Season.Spring : Season.Fall;
#endif

#if false
            int startDistance = (int)midpointOfYear - (int)((distanceFromEquator / 90.0f) * midpointOfYear);

            Season seasonNorthernHemisphere;

            if ((distanceFromSummerHeight >= startDistance) && (distanceFromSummerHeight <= (startDistance + 45)))
            {
                // fall / spring
                if (dayOfYear > midpointOfYear)
                    seasonNorthernHemisphere = Season.Fall;
                else
                    seasonNorthernHemisphere = Season.Spring;
            }
            else
            {
                // summer / winter
                if (distanceFromSummerHeight < startDistance)
                    return Season.Summer;
                else
                    return Season.Winter;
            }

            if (worldLocation.latitudeHemisphere == Hemisphere.North)
                return seasonNorthernHemisphere;

            if (seasonNorthernHemisphere == Season.Spring)
                return Season.Fall;
            else if (seasonNorthernHemisphere == Season.Summer)
                return Season.Winter;
            else if (seasonNorthernHemisphere == Season.Fall)
                return Season.Spring;
            else
                return Season.Summer;
#endif

#if false
            if ((dateTime.Month >= 10) && (dateTime.Month <= 11))
                return (worldLocation.latitudeHemisphere == Hemisphere.North ? Season.Fall : Season.Spring);
            else if ((dateTime.Month == 12) || (dateTime.Month <= 3))
                return (worldLocation.latitudeHemisphere == Hemisphere.North ? Season.Winter : Season.Summer);
            else if ((dateTime.Month >= 4) && (dateTime.Month <= 5))
                return (worldLocation.latitudeHemisphere == Hemisphere.North ? Season.Spring : Season.Fall);
            else
                return (worldLocation.latitudeHemisphere == Hemisphere.North ? Season.Summer : Season.Winter);
#endif
        }

        public Zone getZone(int x, int y)
        {
            WorldLocation worldLocation = getWorldLocation(x, y);

            // Tropical within 25 degrees of the equator.
            if ((worldLocation.latitude <= _latitudeTropicOfCancer) && (worldLocation.latitude >= _latitudeTropicOfCapricorn))
                return Zone.Tropical;

            // Polar within 25 degrees of the poles.
            if ((worldLocation.latitude >= _latitudeArcticCircle) || (worldLocation.latitude <= _latitudeAntarcticCircle))
                return Zone.Polar;

            return Zone.Temperate;
        }
        #endregion

        #region private methods
        #endregion

        public int getAverageTemperature()
        {
            // Factors affecting average temperature:
            // * Latitude (poleward is colder)
            // * Season (northern hemisphere is warmer in July, southern hemisphere is warmer in January)
            // * Elevation (higher elevations are colder, lapse rate of 2C per 1,000 feet)
            // * Land vs. Water (land has more extremes -- hotter in summer, colder in winter -- than water (including marshes and such))
            // * Vegetation (heavier vegetation has less extremes than light or no vegetation)
            // * Prevailing Winds (from bodies of water moderates temperature, from dry land exaggerates it)

            // Leaves change color more due to declining light than average temperature

            // TODO: Develop a WorldModel class that hangs on to all the world stuff...

            // Good resource: http://www.blueplanetbiomes.org/climate.htm
            // Another good resource: http://webinquiry.org/examples/temps/index.htm
            // http://www-das.uwyo.edu/~geerts/cwx/notes/chap16/geo_clim.html

            /*
Elevation

Actual results will vary depending on cities selected, as well as a variety of other factors.

    Temperature = -0.0026 (Elevation in feet) + 77.177°F

Latitude

Actual results will vary depending on cities selected, as well as a variety of other factors.

    For locations below 20°N: Temperature = 80°F.
    For locations between 20°N and 60°N: Temperature = -0.988 (latitude) + 96.827°F.
    For locations above 60°N: Temperature = -2.5826 (latitude) + 193.33°F

Combining Equations

We cannot quite combine two equations together. For example, it will not work to create an equation Temperature = -0.0026 (Elevation) + 77.177 -0.988 (Latitude) + 96.827. By putting 0 in for elevation and 30 in for latitude, we would get:

    Temperature = -0.0025 (0) +77.177 - 0.988 (30) + 96.827 = 144.36 °F!
    That's way to high. The problem is the adding of both 77.177 and 96.827.

We should first use one of the latitude equations to establish what the temperature of a city would be if it were located at sea level. We could then adjust this temperature for the city's elevation.             
             */
            return 72;
        }
    }
}
