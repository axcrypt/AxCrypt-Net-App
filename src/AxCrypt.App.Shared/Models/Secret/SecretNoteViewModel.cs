using AxCrypt.Content;
using System.ComponentModel.DataAnnotations;

namespace AxCrypt.App.Shared.Models.Secret;

public class SecretNoteViewModel : SecretBaseViewModel
{
    public static SecretNoteViewModel Empty = new SecretNoteViewModel("", "");

    public SecretNoteViewModel(string secretDesc, string note) : base(secretDesc)
    {
        SecretDesc = secretDesc;
        Note = note;
    }

    [StringLength(100000)]
    [Display(Name = nameof(Texts.NoteContentPrompt), ResourceType = typeof(Content.Resource))]
    public string Note { get; set; }
}