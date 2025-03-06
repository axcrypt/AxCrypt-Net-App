using AxCrypt.Api.Model;
using AxCrypt.Core.UI;

namespace AxCrypt.App.Shared.Models.Secret;

public class SecretSharedUserViewModel : IEquatable<SecretSharedUserViewModel>
{
    public SecretSharedUserViewModel(EmailAddress userEmail, SecretShareVisibility visibility, string ownerEmail, AccountStatus userAccountStatus = AccountStatus.Unknown)
    {
        UserEmail = userEmail;
        ImageSource = "contact-icon";
        if (userAccountStatus == AccountStatus.Verified)
        {
            ImageSource = "axcrypt-icons";
        }
        OwnerEmail = ownerEmail;
        Visibility = visibility;
    }

    public EmailAddress UserEmail { get; set; }
    public string OwnerEmail { get; set; }
    public SecretShareVisibility Visibility { get; set; }
    public string ImageSource { get; set; }
    public string DotImage { get; } = "DotsIcon.png";

    public bool Equals(SecretSharedUserViewModel? other)
    {
        if ((object)other! == null)
        {
            return false;
        }
        return UserEmail == other.UserEmail && ImageSource == other.ImageSource && DotImage == other.DotImage;
    }

    public override bool Equals(object? obj)
    {
        if (obj == null || typeof(SecretSharedUserViewModel) != obj.GetType())
        {
            return false;
        }
        SecretSharedUserViewModel other = (SecretSharedUserViewModel)obj;
        return Equals(other);
    }

    public override int GetHashCode()
    {
        return UserEmail.GetHashCode() ^ ImageSource.GetHashCode() ^ DotImage.GetHashCode();
    }

    public static bool operator ==(SecretSharedUserViewModel left, SecretSharedUserViewModel right)
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

    public static bool operator !=(SecretSharedUserViewModel left, SecretSharedUserViewModel right)
    {
        return !(left == right);
    }
}