namespace BNHPortalServices
{
    public interface IUserInfoProvider
    {
        public UserInfo UserInfo { get; set; }
    }

    internal class UserInfoProvider : IUserInfoProvider
    {
        public UserInfo UserInfo { get; set; }
    }
}