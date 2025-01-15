using System.Collections.Generic;

namespace AElf.CrossChainServer.Filter;

public static class ResponseDtoExtension
{
    public static ResponseDto ObjectResult(this ResponseDto responseDto, object data)
    {
        responseDto.Code = "20000";
        responseDto.Data = data;
        return responseDto;
    }

    public static ResponseDto NoContent(this ResponseDto responseDto)
    {
        responseDto.Code = "20004";
        responseDto.Message = "No content";
        return responseDto;
    }

    public static ResponseDto EmptyResult(this ResponseDto responseDto)
    {
        responseDto.Code = "20001";
        responseDto.Message = "Empty result";
        return responseDto;
    }

    public static ResponseDto StatusCodeResult(this ResponseDto responseDto, int statusCode, string name)
    {
        responseDto.Code = "20010";
        responseDto.Message = $"State code: {statusCode}, {name}";
        return responseDto;
    }

    public static ResponseDto UnhandledExceptionResult(this ResponseDto responseDto, string code, string message)
    {
        responseDto.Code = code;
        responseDto.Message = message;
        return responseDto;
    }

    public static ValidationResponseDto ValidationResult(
        this ValidationResponseDto responseDto,
        IList<System.ComponentModel.DataAnnotations.ValidationResult> validationErrors)
    {
        responseDto.Code = "-1";
        responseDto.Message = "Your request is not valid!";
        responseDto.ValidationErrors = validationErrors;
        return responseDto;
    }
}