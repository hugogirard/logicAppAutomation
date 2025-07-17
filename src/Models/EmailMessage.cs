using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contoso.Models;

public class SendMailRequest
{
    public Message Message { get; set; } = new();
    public bool SaveToSentItems { get; set; }
}

public class Message
{
    public string Subject { get; set; } = string.Empty;
    public Body Body { get; set; } = new();
    public List<Recipient> ToRecipients { get; set; } = new();
}

public class Body
{
    public string ContentType { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

public class Recipient
{
    public EmailAddress EmailAddress { get; set; } = new();
}

public class EmailAddress
{
    public string Address { get; set; } = string.Empty;
}

