require 'rack'
require './app/controllers/index_site' # Assuming your MyController class is defined in a separate file.

# Add the Rack::Static middleware to serve static files from the 'public' directory.
app = Rack::Builder.new do
    use Rack::Static, root: 'public', urls: ['/css']
  map '/' do
    run MyController
  end
end

Rack::Handler::WEBrick.run app