using System;
using System.Runtime.Serialization;

namespace HttpApi.Exceptions
{
    /// <summary>
    /// Кастомные эксепшены для апи
    /// </summary>
    [Serializable]
    public static class InternalExceptions
    {
        /// <inheritdoc />
        /// <summary>
        /// Не найдено
        /// </summary>
        [Serializable]
        public class NotFoundException : Exception
        {
            /// <inheritdoc />
            /// <summary>
            /// Конструктор с описанием ошибки
            /// </summary>
            /// <param name="error"></param>
            public NotFoundException(string error)
                : base(error)
            {
            }

            /// <inheritdoc />
            /// <summary>
            /// Конструктор с описанием ошибки и экземпляром исключения
            /// </summary>
            /// <param name="error"></param>
            /// <param name="exception"></param>
            public NotFoundException(string error, Exception exception)
                : base(error, exception)
            {
            }

            protected NotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
            {
            }
        }
    }
}