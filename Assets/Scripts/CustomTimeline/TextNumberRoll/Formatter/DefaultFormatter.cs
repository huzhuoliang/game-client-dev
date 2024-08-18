using System.ComponentModel;

namespace CustomTimeline.TextNumberRoll {
    [DisplayName("默认格式化")]
    public class DefaultFormatter : TextNumberFormatter {
        
        public override string FormatNumber(float number) {
            return number.ToString("0.00");
        }

        public override string FormatNumber(int number) {
            return number.ToString();
        }
    }
}
