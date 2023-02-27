namespace MakerBot.Rpc
{
    public class Firmware_Version
    {
        public int major;
        public int minor;
        public int bugfix;
        public int build;

        public override string ToString()
        {
            return string.Format("{0}.{1}.{2}.{3}", this.major, this.minor, this.bugfix, this.build);// base.ToString();
        }
    }
}
