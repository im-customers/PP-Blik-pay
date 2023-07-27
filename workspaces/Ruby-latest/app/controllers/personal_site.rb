# my_controller.rb
require "json"
require "net/http"
require "uri"
require "rack/response"
# require_relative "oauth"
# require_relative "config"

class MyController
  def self.call(env)
    new(env).handle_request
  end

  def initialize(env)
    @request = Rack::Request.new(env)
    @response = Rack::Response.new
  end

  def handle_request
    case @request.path_info
    when "/"
      serve_static_file("./views/index.html")
    when %r{^/capture/(\w+)$}
      capture_order($1)
    when "/webhook"
      handle_webhook
    else
      not_found
    end

    @response.finish
  end

  private

  def serve_static_file("index.html")
    file = File.join(File.dirname(__FILE__), file_path)
    if File.exist?(file)
      @response.write(File.read(file))
      @response.status = 200
      @response.headers["Content-Type"] = {}
    else
      not_found
    end
  end

  def capture_order(order_id)
    access_token = get_access_token
    response_data = send_capture_request(access_token, order_id)
    @response.write(response_data.to_json)
  rescue StandardError
    @response.status = 400
  end

  def handle_webhook
    access_token = get_access_token

    event_type = @request.params["event_type"]
    resource = JSON.parse(@request.body.read)
    order_id = resource["id"]

    # Perform webhook signature verification here (not implemented in this example)

    if event_type == "CHECKOUT.ORDER.APPROVED"
      capture_order(order_id)
    end

    @response.status = 200
  rescue StandardError
    @response.status = 400
  end

  def get_access_token
    # Implement the logic to get the access token from your OAuth mechanism (not implemented in this example)
  end

  def send_capture_request(access_token, order_id)
    url = "#{Sample_API_BASE}/v2/checkout/orders/#{order_id}/capture"
    headers = {
      "Content-Type" => "application/json",
      "Accept" => "application/json",
      "Authorization" => "Bearer #{access_token}",
    }

    uri = URI(url)
    http = Net::HTTP.new(uri.host, uri.port)
    http.use_ssl = true

    request = Net::HTTP::Post.new(uri.path, headers)
    response = http.request(request)

    JSON.parse(response.body)
  end

  def not_found
    @response.status = 404
    @response.write("Not Found")
  end
end
