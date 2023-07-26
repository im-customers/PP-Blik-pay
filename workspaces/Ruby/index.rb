require 'sinatra'
require 'dotenv'
require 'httparty'
require 'json'
require 'launchy'

require_relative './oauth'
require_relative './config'


# set :port, ENV['PORT'] || 4567
set :public_folder, File.dirname(__FILE__) + '/views'
# set :bind, 'localhost'

get '/' do
  erb :index
end

# Capture Order handler
post '/capture/:orderId' do
  order_id = params[:orderId]

  access_token = get_access_token[:access_token]

  response = HTTParty.post(
    "#{PAYPAL_API_BASE}/v2/checkout/orders/#{order_id}/capture",
    headers: {
      'Content-Type' => 'application/json',
      'Accept' => 'application/json',
      'Authorization' => "Bearer #{access_token}",
    }
  )

  puts '💰 Payment captured!'
  response.body
end

# Webhook handler
post '/webhook' do
  access_token = get_access_token[:access_token]

  event_type = params[:event_type]
  resource = JSON.parse(request.body.read)
  order_id = resource['id']

  puts '🪝 Received Webhook Event'

  # Verify the webhook signature
  begin
    response = HTTParty.post(
      "#{PAYPAL_API_BASE}/v1/notifications/verify-webhook-signature",
      headers: {
        'Content-Type' => 'application/json',
        'Accept' => 'application/json',
        'Authorization' => "Bearer #{access_token}",
      },
      body: {
        transmission_id: request.env['PAYPAL-transmission-id'],
        transmission_time: request.env['PAYPAL-transmission-time'],
        cert_url: request.env['PAYPAL-cert-url'],
        auth_algo: request.env['PAYPAL-auth-algo'],
        transmission_sig: request.env['PAYPAL-transmission-sig'],
        webhook_id: WEBHOOK_ID,
        webhook_event: resource,
      }.to_json
    )

    verification_status = JSON.parse(response.body)['verification_status']

    unless verification_status == 'SUCCESS'
      puts '⚠️  Webhook signature verification failed.'
      return 400
    end
  rescue StandardError
    puts '⚠️  Webhook signature verification failed.'
    return 400
  end

  # Capture the order
  if event_type == 'CHECKOUT.ORDER.APPROVED'
    begin
      response = HTTParty.post(
        "#{PAYPAL_API_BASE}/v2/checkout/orders/#{order_id}/capture",
        headers: {
          'Content-Type' => 'application/json',
          'Accept' => 'application/json',
          'Authorization' => "Bearer #{access_token}",
        }
      )

      puts '💰 Payment captured!'
    rescue StandardError
      puts '❌ Payment failed.'
      return 400
    end
  end

  200
end

