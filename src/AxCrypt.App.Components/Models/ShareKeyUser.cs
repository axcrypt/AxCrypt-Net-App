using AxCrypt.Api.Model;
using AxCrypt.Core.UI;

namespace AxCrypt.App.Components.Models
{
    public class ShareKeyUser : IEquatable<ShareKeyUser>
    {
        public ShareKeyUser(EmailAddress userEmail, AccountStatus userAccountStatus)
        {
            UserEmail = userEmail;
            Image = "";
            if (userAccountStatus == AccountStatus.Verified)
            {
                Image = "";
            }
        }

        public EmailAddress UserEmail { get; set; }

        public string Image { get; set; }

        public string DotImage { get; } = "";

        public bool Equals(ShareKeyUser other)
        {
            if ((object)other == null)
            {
                return false;
            }

            return UserEmail == other.UserEmail && Image == other.Image && DotImage == other.DotImage;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || typeof(ShareKeyUser) != obj.GetType())
            {
                return false;
            }
            ShareKeyUser other = (ShareKeyUser)obj;

            return Equals(other);
        }

        public override int GetHashCode()
        {
            return UserEmail.GetHashCode() ^ Image.GetHashCode() ^ DotImage.GetHashCode();
        }

        public static bool operator ==(ShareKeyUser left, ShareKeyUser right)
        {
            if (Object.ReferenceEquals(left, right))
            {
                return true;
            }
            if ((object)left == null)
            {
                return false;
            }
            return left.Equals(right);
        }

        public static bool operator !=(ShareKeyUser left, ShareKeyUser right)
        {
            return !(left == right);
        }
    }
}