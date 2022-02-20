using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utilities
{
    public delegate DateTimeOffset DateRoundFunctionDelegate(DateTimeOffset value, RoundDateDirection direction);
    public enum RoundDateDirection
    {
        UP,
        DOWN,
        CLOSEST
    }
    public static class DateTimeHelper
    {
        public static DateTimeOffset GetFirstOfMonth(DateTimeOffset inDate)
        {
            return CreateDateFromParts(inDate.Year, inDate.Month, 1, 0, 0, 0);
        }
        public static DateTimeOffset CreateDateFromParts(int year, int month, int day, int hour, int min, int sec)
        {
            // deals with the problems from rounding functions
            int yearToUse = year;
            int monthToUse = month;
            int dayToUse = day;
            int hourToUse = hour;
            int minToUse = min;
            int secToUse = sec;


            if (month == 13)
            {
                monthToUse = 1;
                yearToUse++;
            }
            try
            {
                return new DateTimeOffset(yearToUse, monthToUse, dayToUse, hourToUse, minToUse, secToUse,
                                    TimeZoneInfo.Utc.BaseUtcOffset);
            }
            catch (Exception ex)
            {
                // instead of trying to code all possible issues up-front
                // just fix them one at a time as they come up
                StringBuilder message = new StringBuilder();
                message.AppendLine("Error creating dates from parts.");
                message.AppendLine(string.Format("Year: {0}", year));
                message.AppendLine(string.Format("Month: {0}", month));
                message.AppendLine(string.Format("Day: {0}", day));
                message.AppendLine(string.Format("Hour: {0}", hour));
                message.AppendLine(string.Format("Minute: {0}", min));
                message.AppendLine(string.Format("Second: {0}", sec));
                throw new ArgumentOutOfRangeException(message.ToString(), ex);
            }
        }
        public static DateTimeOffset DontRound(DateTimeOffset value, RoundDateDirection direction)
        {
            // this is here for when we have to return a delegate
            return value;
        }
        public static DateTimeOffset RoundToSecond(DateTimeOffset value, RoundDateDirection direction)
        {
            return RoundDateTimeTicks(value, new TimeSpan(0, 0, 1));
        }
        public static DateTimeOffset RoundToMinute(DateTimeOffset value, RoundDateDirection direction)
        {
            return RoundDateTimeTicks(value, new TimeSpan(0, 1, 0));
        }
        public static DateTimeOffset RoundTo5Minute(DateTimeOffset value, RoundDateDirection direction)
        {
            return RoundToNMinutes(value, 5, direction);
        }
        public static DateTimeOffset RoundTo30Minute(DateTimeOffset value, RoundDateDirection direction)
        {
            return RoundToNMinutes(value, 30, direction);
        }
        public static DateTimeOffset RoundToHour(DateTimeOffset value, RoundDateDirection direction)
        {
            if (direction == RoundDateDirection.UP)
            {
                return CreateDateFromParts(value.Year, value.Month, value.Day,
                            value.Hour + 1, 0, 0);
            }
            if (direction == RoundDateDirection.DOWN)
            {
                return CreateDateFromParts(value.Year, value.Month, value.Day,
                            0, 0, 0);
            }
            if (direction == RoundDateDirection.CLOSEST)
            {
                int hour = (value.Minute >= 30) ? value.Hour + 1 : value.Hour;
                return CreateDateFromParts(value.Year, value.Month, value.Day,
                            hour, 0, 0);
            }
            throw new NotImplementedException();
        }
        public static DateTimeOffset RoundTo4Hour(DateTimeOffset value, RoundDateDirection direction)
        {
            return RoundToNHours(value, 4, direction);
        }
        public static DateTimeOffset RoundTo12Hour(DateTimeOffset value, RoundDateDirection direction)
        {
            return RoundToNHours(value, 12, direction);
        }
        public static DateTimeOffset RoundToDay(DateTimeOffset value, RoundDateDirection direction)
        {
            if (direction == RoundDateDirection.UP)
            {
                return CreateDateFromParts(value.Year, value.Month, value.Day + 1,
                            0, 0, 0);
            }
            if (direction == RoundDateDirection.DOWN)
            {
                return CreateDateFromParts(value.Year, value.Month, value.Day,
                            0, 0, 0);
            }
            if (direction == RoundDateDirection.CLOSEST)
            {
                int day = (value.Hour >= 12) ? value.Day + 1 : value.Day;
                return CreateDateFromParts(value.Year, value.Month, day,
                            0, 0, 0);
            }
            throw new NotImplementedException();
        }
        public static DateTimeOffset RoundToWeek(DateTimeOffset value, RoundDateDirection direction)
        {
            int weekDayNum = Convert.ToInt32(value.DayOfWeek);
            int monthDayNum = value.Day;
            int daysTillNextSunday = 7 - weekDayNum;
            int nextSunday = monthDayNum + (daysTillNextSunday);
            int daysTillLastSunday = 0 - weekDayNum;
            int lastSunday = monthDayNum + (daysTillLastSunday);

            if (direction == RoundDateDirection.UP)
            {

                return CreateDateFromParts(value.Year, value.Month, nextSunday,
                            0, 0, 0);
            }
            if (direction == RoundDateDirection.DOWN)
            {
                return CreateDateFromParts(value.Year, value.Month, lastSunday,
                            0, 0, 0);
            }
            if (direction == RoundDateDirection.CLOSEST)
            {
                if (weekDayNum <= 3)
                {
                    return CreateDateFromParts(value.Year, value.Month, lastSunday,
                            0, 0, 0);
                }
                else
                {
                    return CreateDateFromParts(value.Year, value.Month, nextSunday,
                            0, 0, 0);
                }

            }
            throw new NotImplementedException();
        }
        public static DateTimeOffset RoundToHalfMonth(DateTimeOffset value, RoundDateDirection direction)
        {
            if (direction == RoundDateDirection.UP)
            {
                if (value.Day < 15)
                {
                    return CreateDateFromParts(value.Year, value.Month, 15,
                                0, 0, 0);
                }
                else
                {
                    return CreateDateFromParts(value.Year, value.Month + 1, 1,
                                0, 0, 0);
                }
            }
            if (direction == RoundDateDirection.DOWN)
            {
                if (value.Day < 15)
                {
                    return CreateDateFromParts(value.Year, value.Month, 1,
                                0, 0, 0);
                }
                else
                {
                    return CreateDateFromParts(value.Year, value.Month, 15,
                                0, 0, 0);
                }
            }
            if (direction == RoundDateDirection.CLOSEST)
            {
                if (value.Day < 8)
                {
                    return CreateDateFromParts(value.Year, value.Month, 1,
                                0, 0, 0);
                }
                else if (value.Day < 15)
                {
                    return CreateDateFromParts(value.Year, value.Month, 15,
                                0, 0, 0);
                }
                else if (value.Day < 23)
                {
                    return CreateDateFromParts(value.Year, value.Month, 15,
                                0, 0, 0);
                }
                else
                {
                    return CreateDateFromParts(value.Year, value.Month + 1, 1,
                                0, 0, 0);
                }
            }
            throw new NotImplementedException();
        }
        public static DateTimeOffset RoundToMonth(DateTimeOffset value, RoundDateDirection direction)
        {
            if (direction == RoundDateDirection.UP)
            {
                return CreateDateFromParts(value.Year, value.Month + 1, 1,
                            0, 0, 0);
            }
            if (direction == RoundDateDirection.DOWN)
            {
                return CreateDateFromParts(value.Year, value.Month, 1,
                            0, 0, 0);
            }
            if (direction == RoundDateDirection.CLOSEST)
            {
                if (value.Day < 15)
                {
                    return CreateDateFromParts(value.Year, value.Month, 1,
                            0, 0, 0);
                }
                else
                {
                    try
                    {
                        return CreateDateFromParts(value.Year, value.Month + 1, 1,
                                            0, 0, 0);
                    }
                    catch (Exception)
                    {

                        throw;
                    }
                }

            }
            throw new NotImplementedException();
        }
        public static DateTimeOffset RoundToHalfYear(DateTimeOffset value, RoundDateDirection direction)
        {
            if (direction == RoundDateDirection.UP)
            {
                if (value.Month < 7)
                {
                    return CreateDateFromParts(value.Year, 7, 1,
                            0, 0, 0);
                }
                else
                {
                    return CreateDateFromParts(value.Year + 1, 1, 1,
                            0, 0, 0);
                }
            }
            if (direction == RoundDateDirection.DOWN)
            {
                if (value.Month < 7)
                {
                    return CreateDateFromParts(value.Year, 1, 1,
                            0, 0, 0);
                }
                else
                {
                    return CreateDateFromParts(value.Year, 7, 1,
                            0, 0, 0);
                }
            }
            if (direction == RoundDateDirection.CLOSEST)
            {
                if (value.Month < 4)
                {
                    return CreateDateFromParts(value.Year, 1, 1,
                            0, 0, 0);
                }
                if (value.Month < 7)
                {
                    return CreateDateFromParts(value.Year, 7, 1,
                            0, 0, 0);
                }
                if (value.Month < 10)
                {
                    return CreateDateFromParts(value.Year, 7, 1,
                            0, 0, 0);
                }
                else
                {
                    return CreateDateFromParts(value.Year + 1, 1, 1,
                            0, 0, 0);
                }

            }
            throw new NotImplementedException();
        }
        public static DateTimeOffset RoundToYear(DateTimeOffset value, RoundDateDirection direction)
        {
            if (direction == RoundDateDirection.UP)
            {
                return CreateDateFromParts(value.Year + 1, 1, 1,
                            0, 0, 0);
            }
            if (direction == RoundDateDirection.DOWN)
            {
                return CreateDateFromParts(value.Year, 1, 1,
                            0, 0, 0);
            }
            if (direction == RoundDateDirection.CLOSEST)
            {
                if (value.Month < 7)
                {
                    return CreateDateFromParts(value.Year, 1, 1,
                            0, 0, 0);
                }
                else
                {
                    return CreateDateFromParts(value.Year + 1, 1, 1,
                            0, 0, 0);
                }

            }
            throw new NotImplementedException();
        }
        public static DateTimeOffset RoundTo5Year(DateTimeOffset value, RoundDateDirection direction)
        {
            return RoundToNYears(value, 5, direction);
        }
        public static DateTimeOffset RoundTo10Year(DateTimeOffset value, RoundDateDirection direction)
        {
            return RoundToNYears(value, 10, direction);
        }
        public static DateTimeOffset RoundTo20Year(DateTimeOffset value, RoundDateDirection direction)
        {
            return RoundToNYears(value, 20, direction);
        }


        private static DateTimeOffset RoundDateTimeTicks(DateTimeOffset value, TimeSpan interval)
        {
            var halfIntervalTicks = (value.Ticks + 1) >> 1;

            return value.AddTicks(halfIntervalTicks - ((value.Ticks + halfIntervalTicks) % interval.Ticks));
        }
        private static DateTimeOffset RoundToNMinutes(DateTimeOffset value, int n, RoundDateDirection direction)
        {
            int modVal = value.Minute % n;
            int newValue = value.Minute;
            if (direction == RoundDateDirection.UP)
            {
                if (value.Minute >= 60 - n)
                {
                    return CreateDateFromParts(value.Year, value.Month, value.Day,
                            value.Hour + 1, 0, 0);
                }
                newValue += (n - modVal);

            }
            if (direction == RoundDateDirection.DOWN)
            {
                newValue -= modVal;

            }
            if (direction == RoundDateDirection.CLOSEST)
            {
                if (modVal >= n / 2) return RoundToNMinutes(value, n, RoundDateDirection.UP);
                else return RoundToNMinutes(value, n, RoundDateDirection.DOWN);
            }
            return CreateDateFromParts(value.Year, value.Month, value.Day,
                            value.Hour, newValue, 0);
        }
        private static DateTimeOffset RoundToNHours(DateTimeOffset value, int n, RoundDateDirection direction)
        {
            int modVal = value.Hour % n;
            int newValue = value.Minute;
            if (direction == RoundDateDirection.UP)
            {
                if (value.Minute >= 23 - n)
                {
                    return CreateDateFromParts(value.Year, value.Month, value.Day + 1,
                            0, 0, 0);
                }
                newValue += (n - modVal);

            }
            if (direction == RoundDateDirection.DOWN)
            {
                newValue -= modVal;

            }
            if (direction == RoundDateDirection.CLOSEST)
            {
                if (modVal >= n / 2) return RoundToNHours(value, n, RoundDateDirection.UP);
                else return RoundToNHours(value, n, RoundDateDirection.DOWN);
            }
            return CreateDateFromParts(value.Year, value.Month, value.Day,
                            newValue, newValue, 0);
        }
        private static DateTimeOffset RoundToNYears(DateTimeOffset value, int n, RoundDateDirection direction)
        {
            int modVal = value.Year % n;
            int newYear = value.Year;
            if (direction == RoundDateDirection.UP)
            {
                newYear += (n - modVal);

            }
            if (direction == RoundDateDirection.DOWN)
            {
                newYear -= modVal;

            }
            if (direction == RoundDateDirection.CLOSEST)
            {
                if (modVal >= n / 2) return RoundToNYears(value, n, RoundDateDirection.UP);
                else return RoundToNYears(value, n, RoundDateDirection.DOWN);
            }
            return CreateDateFromParts(newYear, 1, 1,
                            0, 0, 0);
        }

    }
}
