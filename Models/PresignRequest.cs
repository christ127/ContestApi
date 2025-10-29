using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace ContestApi.Models;

public record PresignRequest(
    [Required] string FileName,
    [Required] string ContentType,
    [Required] long Bytes
);