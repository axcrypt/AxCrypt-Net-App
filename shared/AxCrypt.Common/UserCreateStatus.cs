using System;
using System.Collections.Generic;
using System.Text;

namespace AxCrypt.Common
{
    public enum UserCreateStatus
    {
        //
        // Summary:
        //     The user was successfully created.
        Success,

        //
        // Summary:
        //     The user name was not found in the database.
        InvalidUserName,

        //
        // Summary:
        //     The password is not formatted correctly.
        InvalidPassword,

        //
        // Summary:
        //     The password question is not formatted correctly.
        InvalidQuestion,

        //
        // Summary:
        //     The password answer is not formatted correctly.
        InvalidAnswer,

        //
        // Summary:
        //     The email address is not formatted correctly.
        InvalidEmail,

        //
        // Summary:
        //     The user name already exists in the database for the application.
        DuplicateUserName,

        //
        // Summary:
        //     The email address already exists in the database for the application.
        DuplicateEmail,

        //
        // Summary:
        //     The user was not created, for a reason defined by the provider.
        UserRejected,

        //
        // Summary:
        //     The provider user key is of an invalid type or format.
        InvalidProviderUserKey,

        //
        // Summary:
        //     The provider user key already exists in the database for the application.
        DuplicateProviderUserKey,

        //
        // Summary:
        //     The provider returned an error that is not described by other System.Web.Security.MembershipCreateStatus
        //     enumeration values.
        ProviderError
    }
}