using LinuxGPIB.Net.Abstractions;

namespace LinuxGPIB.Net;

public static class GpibDeviceExtensions
{
    /// Reads a response from the device and converts it to <typeparamref name="T"/>.
    /// The underlying device implementation is expected to return an ASCII string
    /// with any protocol terminators already trimmed.
    /// <remarks>
    /// <para>
    ///     The response is read as an ASCII string and converted to the requested
    ///     type <typeparamref name="T"/> using <see cref="CultureInfo.InvariantCulture"/>.
    ///     The following conversions are supported:
    /// </para>
    /// <list type="bullet">
    ///     <item>
    ///     <description>
    ///         <see cref="string"/> — the raw response is returned unchanged.
    ///     </description>
    ///     </item>
    ///     <item>
    ///     <description>
    ///         Numeric types (<see cref="int"/>, <see cref="long"/>,
    ///         <see cref="double"/>, <see cref="decimal"/>, etc.) — the response
    ///         must contain a valid numeric representation in invariant culture
    ///         format (e.g. <c>"1.234"</c>).
    ///     </description>
    ///     </item>
    ///     <item>
    ///     <description>
    ///         <see cref="bool"/> — common SCPI boolean formats are recognized:
    ///         <c>"1"</c>, <c>"0"</c>, <c>"ON"</c>, <c>"OFF"</c>,
    ///         <c>"TRUE"</c>, <c>"FALSE"</c> (case-insensitive).
    ///         Any other format will result in a <see cref="FormatException"/>.
    ///    </description>
    ///     </item>
    /// </list>
    /// </remarks>
    public static T Read<T>(this IGpibDevice device)
    {
        var response = device.Read();
        return response.ConvertScpiTo<T>();
    }

    /// <summary>
    /// Writes a command to the device and reads back the output as <typeparamref name="T"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    ///     The response is read as an ASCII string and converted to the requested
    ///     type <typeparamref name="T"/> using <see cref="CultureInfo.InvariantCulture"/>.
    ///     The following conversions are supported:
    /// </para>
    /// <list type="bullet">
    ///     <item>
    ///     <description>
    ///         <see cref="string"/> — the raw response is returned unchanged.
    ///     </description>
    ///     </item>
    ///     <item>
    ///     <description>
    ///         Numeric types (<see cref="int"/>, <see cref="long"/>,
    ///         <see cref="double"/>, <see cref="decimal"/>, etc.) — the response
    ///         must contain a valid numeric representation in invariant culture
    ///         format (e.g. <c>"1.234"</c>).
    ///     </description>
    ///     </item>
    ///     <item>
    ///     <description>
    ///         <see cref="bool"/> — common SCPI boolean formats are recognized:
    ///         <c>"1"</c>, <c>"0"</c>, <c>"ON"</c>, <c>"OFF"</c>,
    ///         <c>"TRUE"</c>, <c>"FALSE"</c> (case-insensitive).
    ///         Any other format will result in a <see cref="FormatException"/>.
    ///    </description>
    ///     </item>
    /// </list>
    /// </remarks>
    public static T Query<T>(this IGpibDevice device, string query)
    {
        var response = device.Query(query);
        return response.ConvertScpiTo<T>();
    }

    /// <summary>
    /// Writes a command to the device and reads back the output as <typeparamref name="string"/>.
    /// </summary>
    public static string Query(this IGpibDevice device, string query)
    {
        device.Write(query);
        return device.Read();
    }

    /// <summary>
    /// Writes a query to the device and waits for the device to complete the query then reads back the output as <typeparamref name="T"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    ///     The response is read as an ASCII string and converted to the requested
    ///     type <typeparamref name="T"/> using <see cref="CultureInfo.InvariantCulture"/>.
    ///     The following conversions are supported:
    /// </para>
    /// <list type="bullet">
    ///     <item>
    ///     <description>
    ///         <see cref="string"/> — the raw response is returned unchanged.
    ///     </description>
    ///     </item>
    ///     <item>
    ///     <description>
    ///         Numeric types (<see cref="int"/>, <see cref="long"/>,
    ///         <see cref="double"/>, <see cref="decimal"/>, etc.) — the response
    ///         must contain a valid numeric representation in invariant culture
    ///         format (e.g. <c>"1.234"</c>).
    ///     </description>
    ///     </item>
    ///     <item>
    ///     <description>
    ///         <see cref="bool"/> — common SCPI boolean formats are recognized:
    ///         <c>"1"</c>, <c>"0"</c>, <c>"ON"</c>, <c>"OFF"</c>,
    ///         <c>"TRUE"</c>, <c>"FALSE"</c> (case-insensitive).
    ///         Any other format will result in a <see cref="FormatException"/>.
    ///    </description>
    ///     </item>
    /// </list>
    /// </remarks>
    /// <param name="pollIntervalMs">The delay (in milliseconds) between each poll request. Defaults to 50ms.</param>
    /// <param name="timeoutMs">The maximum time (in milliseconds) to wait for the message.</param>
    /// <exception cref="TimeoutException">Thrown if the message does not become available within the timeout period.</exception>
    public static async Task<T> QueryAsync<T>(
    this IGpibDevice device,
    string query,
    CancellationToken cancellationToken = default,
    int pollIntervalMs = GpibDevice.PollIntervalMs,
    int timeoutMs = GpibDevice.TimeoutMs)
    {
        var response = await device.QueryAsync(query, cancellationToken, pollIntervalMs, timeoutMs);
        return response.ConvertScpiTo<T>();
    }

    /// <summary>
    /// Writes a query to the device and waits for the device to complete the query then reads back the output as <typeparamref name="string"/>.
    /// </summary>
    /// <param name="pollIntervalMs">The delay (in milliseconds) between each poll request. Defaults to 50ms.</param>
    /// <param name="timeoutMs">The maximum time (in milliseconds) to wait for the message.</param>
    /// <exception cref="TimeoutException">Thrown if the message does not become available within the timeout period.</exception>
    public static async Task<string> QueryAsync(
    this IGpibDevice device,
    string query,
    CancellationToken cancellationToken = default,
    int pollIntervalMs = GpibDevice.PollIntervalMs,
    int timeoutMs = GpibDevice.TimeoutMs)
    {
        device.Write(query);
        await device.WaitForMessageAsync(cancellationToken, pollIntervalMs, timeoutMs).ConfigureAwait(false);
        return device.Read();
    }
}