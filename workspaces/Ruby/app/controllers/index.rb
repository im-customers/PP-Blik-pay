# my_controller.rb
require "json"
require "net/http"
require "uri"
require 'rack'
require_relative "oauth"
require_relative "config"

class IndexController
  def self.call(env)
    case env["PATH_INFO"]
    when '/' then index
    when %r{^/capture/(\w+)$} then capture_order($1)
    when '/webhook' then handle_webhook
    else
    not_found
    end
  end

  def initialize(env)
    @request = Rack::Request.new(env)
    @response = Rack::Response.new
  end

  def self.index
    [200, { 'Content-Type' => 'text/html' }, [File.read("./app/views/index.html")]]
  end

  def self.capture_order(order_id)
    puts 'Blik Response: ' + order_id
    access_token = get_access_token
    response_data = send_capture_request(access_token, order_id)
    puts 'Blik Response: ' + response_data
    @response.write(response_data.to_json)
  rescue StandardError
    @response.status = 400
  end

  def self.handle_webhook
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

  def send_capture_request(access_token, order_id)
    url = "#{PAYPAY_API_BASE}/v2/checkout/orders/#{order_id}/capture"
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
    puts '💰 Payment captured!'
    JSON.parse(response.body)
  end

  def not_found
    @response.status = 404
    @response.write("Not Found")
  end
end
