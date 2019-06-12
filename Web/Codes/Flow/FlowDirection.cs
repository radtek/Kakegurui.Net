
namespace Kakegurui.Web.Codes.Flow
{
    /// <summary>
    /// 车道流向
    /// </summary>
    public enum FlowDirection
    {
        未知 = 0,
        直行 = 11,
        左转 = 12,
        右转 = 13,
        直左 = 21,
        直右 = 22,
        左右 = 23,
        直左右 = 24,
        调头 = 31,
        其它 = 99
    }
}
