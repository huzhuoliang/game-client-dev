namespace CustomTimeline.TextNumberRoll {
    public abstract class TextNumberFormatter {
        public virtual string FormatNumber(int number) {
            return "";
        }

        public virtual string FormatNumber(float number) {
            return "";
        }

        public virtual string FormatNumber(double number) {
            return "";
        }
    }
}
