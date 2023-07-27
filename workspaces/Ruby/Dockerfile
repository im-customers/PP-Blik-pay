# .devcontainer/Dockerfile

# Use the official Ruby image as the base image
FROM ruby:latest

# Set the working directory inside the container
WORKDIR /Ruby

# Install system dependencies
RUN apt-get update && \
    apt-get install -y build-essential

# Install the Bundler gem globally
RUN gem install bundler

# Copy the project files into the container
COPY . /Ruby/

# Install project dependencies
RUN bundle install

# Expose the port used by the Sinatra app (replace 4567 with the appropriate port if needed)
EXPOSE 4567

# Start the development server when the container is run
CMD ["bundle", "exec", "ruby", "index.rb", "-o", "0.0.0.0"]
